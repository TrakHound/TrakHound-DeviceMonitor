// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using MTConnectClients = MTConnect.Clients;
using MTConnectError = MTConnect.MTConnectError;

namespace TrakHound.TempServer.MTConnect.DeviceFinder
{
    /// <summary>
    /// Handles finding new MTConnect Devices on a network
    /// </summary>
    public class MTConnectDeviceFinder
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private object _lock = new object();
        private ManualResetEvent stop;
        private Stopwatch stopwatch;
        private PingQueue pingQueue;
        private int sentPortRequests = 0;
        private int receivedPortRequests = 0;

        public delegate void DeviceHandler(MTConnectDeviceFinder sender, MTConnectDevice device);
        public delegate void RequestStatusHandler(MTConnectDeviceFinder sender, long milliseconds);
        public delegate void PingSentHandler(MTConnectDeviceFinder sender, IPAddress address);
        public delegate void PingReceivedHandler(MTConnectDeviceFinder sender, IPAddress address, PingReply reply);
        public delegate void PortRequestHandler(MTConnectDeviceFinder sender, IPAddress address, int port);
        public delegate void ProbeRequestHandler(MTConnectDeviceFinder sender, IPAddress address, int port);

        public event PingSentHandler PingSent;
        public event PingReceivedHandler PingReceived;
        public event PortRequestHandler PortOpened;
        public event PortRequestHandler PortClosed;
        public event ProbeRequestHandler ProbeSent;
        public event ProbeRequestHandler ProbeSuccessful;
        public event ProbeRequestHandler ProbeError;

        private class ProbeSender
        {
            public ProbeSender(IPAddress address, int port)
            {
                Address = address;
                Port = port;
            }

            public IPAddress Address { get; set; }
            public int Port { get; set; }
        }

        [XmlAttribute("interfaceId")]
        [JsonProperty("interfaceId")]
        public string InterfaceId { get; set; }

        [XmlAttribute("interfaceDescription")]
        [JsonProperty("interfaceDescription")]
        public string InterfaceDescription { get; set; }

        /// <summary>
        /// Gets or Sets the interval at which the network is scanned for new MTConnect Devices
        /// </summary>
        [XmlAttribute("scanInterval")]
        [JsonProperty("scanInterval")]
        public int ScanInterval { get; set; }

        /// <summary>
        /// Range of Ports to scan
        /// </summary>
        [XmlElement("Ports")]
        [JsonProperty("ports")]
        public PortRange Ports { get; set; }

        /// <summary>
        /// Range of IP Addresses to scan
        /// </summary>
        [XmlElement("Addresses")]
        [JsonProperty("addresses")]
        public AddressRange Addresses { get; set; }

        /// <summary>
        /// The timeout used for requests
        /// </summary>
        [XmlAttribute("timeout")]
        [JsonProperty("timeout")]
        public int Timeout { get; set; }

        /// <summary>
        /// The delay in milliseconds between ping requests
        /// </summary>
        [XmlAttribute("subsequentPingDelay")]
        [JsonProperty("subsequentPingDelay")]
        public int SubsequentPingDelay { get; set; }

        /// <summary>
        /// The range of ports to scan for MTConnect Agents at
        /// </summary>
        [XmlIgnore]
        public int[] PortRange { get; set; }

        /// <summary>
        /// The range of IP Addresses to scan for MTConnect Agents at
        /// </summary>
        [XmlIgnore]
        public IPAddress[] AddressRange { get; set; }

        /// <summary>
        /// Event raised when an MTConnect Device has been found
        /// </summary>
        public event DeviceHandler DeviceFound;

        /// <summary>
        /// Raised when all scanning has finished
        /// </summary>
        public event RequestStatusHandler SearchCompleted;


        public MTConnectDeviceFinder()
        {
            Init();
        }

        public MTConnectDeviceFinder(string interfaceId, string interfaceDescription)
        {
            InterfaceId = interfaceId;
            InterfaceDescription = interfaceDescription;

            Init();
        }

        private void Init()
        {
            Timeout = 500;
            SubsequentPingDelay = 25;
        }

        /// <summary>
        /// Start the DeviceFinder
        /// </summary>
        public void Start(bool async = true)
        {
            var ports = GetPortRange();
            if (ports != null) PortRange = ports;
            else InitializePortRange();

            var ips = GetAddressRange();
            if (ips != null) AddressRange = ips;
            else AddressRange = GetDefaultAddresses();

            stop = new ManualResetEvent(false);

            ThreadPool.QueueUserWorkItem(new WaitCallback(Worker));

            if (!async) while (!stop.WaitOne(1000, true)) { }
        }

        /// <summary>
        /// Stop the DeviceFinder
        /// </summary>
        public void Stop()
        {
            if (stop != null) stop.Set();

            if (pingQueue != null) pingQueue.Stop();

            log.Info("Device Finder Stopped");
        }


        private void Worker(object obj)
        {
            do
            {
                if (AddressRange != null && PortRange != null)
                {
                    var startAddress = AddressRange[0];
                    var endAddress = AddressRange[AddressRange.Length - 1];
                    var startPort = PortRange[0];
                    var endPort = PortRange[PortRange.Length - 1];

                    log.Info(string.Format("Searching for MTConnect Devices on {0} : Addresses : {1} to {2} on Ports {3}-{4}", InterfaceDescription, startAddress, endAddress, startPort, endPort));
                }

                //stop = new ManualResetEvent(false);

                stopwatch = new Stopwatch();
                stopwatch.Start();

                PingQueue_Start();

                // Only repeat if ScanInterval is set
                if (ScanInterval <= 0) break;

            } while (!stop.WaitOne(ScanInterval, true));
        }

        #region "PingQueue"

        private void PingQueue_Start()
        {
            pingQueue = new PingQueue();
            pingQueue.Add(AddressRange.ToList());
            pingQueue.Completed += Queue_Completed;
            pingQueue.PingSent += PingQueue_PingSent;
            pingQueue.PingReceived += Queue_PingReceived;
            pingQueue.Start();
        }

        private void PingQueue_PingSent(IPAddress address)
        {
            PingSent?.Invoke(this, address);
        }

        private void Queue_PingReceived(IPAddress address, PingReply reply)
        {
            PingReceived?.Invoke(this, address, reply);
        }

        private void Queue_Completed(List<IPAddress> successfulAddresses)
        {
            if (successfulAddresses.Count > 0) RunProbeRequests(successfulAddresses);
            else CheckRequestsStatus();
        }

        #endregion

        private void RunProbeRequests(List<IPAddress> addresses)
        {
            foreach (var address in addresses)
            {
                if (stop.WaitOne(0, true)) break;

                foreach (int port in PortRange)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
                    {
                        lock (_lock) sentPortRequests++;

                        if (TestPort(address, port)) SendProbe(address, port);

                        lock (_lock)
                        {
                            receivedPortRequests++;
                            CheckRequestsStatus();
                        }
                    }));

                    if (stop.WaitOne(0, true)) break;
                }
            }
        }

        private void CheckRequestsStatus()
        {
            if (receivedPortRequests >= sentPortRequests)
            {
                long m = 0;

                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    m = stopwatch.ElapsedMilliseconds;
                }

                if (ScanInterval == 0 && stop != null) stop.Set();
                SearchCompleted?.Invoke(this, m);
            }
        }

        private bool TestPort(IPAddress address, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(address, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(Timeout);
                    if (!success)
                    {
                        PortClosed?.Invoke(this, address, port);
                        return false;
                    }
                    else
                    {
                        PortOpened?.Invoke(this, address, port);
                    }

                    client.EndConnect(result);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void SendProbe(IPAddress address, int port)
        {
            try
            {
                var uri = new UriBuilder("http", address.ToString(), port);

                var probe = new MTConnectClients.Probe(uri.ToString());
                probe.UserObject = new ProbeSender(address, port);
                probe.Timeout = Timeout;
                probe.Error += Probe_Error;

                // Notify that a new Probe request has been sent
                ProbeSent?.Invoke(this, address, port);

                var document = probe.Execute();
                if (document != null)
                {
                    // Get the MAC Address of the sender
                    var macAddress = GetMacAddress(address);

                    foreach (var device in document.Devices)
                    {
                        // Notify that a Device was found
                        DeviceFound?.Invoke(this, new MTConnectDevice(address, port, macAddress, device.Name));
                    }

                    // Notify that the Probe reqeuest was successful
                    ProbeSuccessful?.Invoke(this, address, port);
                }
            }
            catch { }
        }

        private void Probe_Error(MTConnectError.Document errorDocument)
        {
            if (errorDocument != null)
            {
                var sender = errorDocument.UserObject as ProbeSender;
                if (sender != null)
                {
                    ProbeError?.Invoke(this, sender.Address, sender.Port);
                }
            }
        }

        private int[] GetPortRange()
        {
            if (Ports != null)
            {
                var l = new List<int>();

                // Add Allowed Ports
                if (Ports.AllowedPorts != null) l.AddRange(Ports.AllowedPorts);

                for (int i = Ports.Minimum; i <= Ports.Maximum; i++)
                {
                    bool allow = true;

                    // Check if in Denied list
                    if (Ports.DeniedPorts != null) allow = !Ports.DeniedPorts.ToList().Exists(o => o == i);

                    // Check if already added to list
                    allow = allow && !l.Exists(o => o == i);

                    if (allow) l.Add(i);
                }

                return l.ToArray();
            }

            return null;
        }

        private IPAddress[] GetAddressRange()
        {
            if (Addresses != null)
            {
                var l = new List<IPAddress>();

                // Add Allowed Ports
                if (Addresses.AllowedAddresses != null) l.AddRange(GetIpAddressFromString(Addresses.AllowedAddresses));

                IPAddress min;
                IPAddress max;

                IPAddress.TryParse(Addresses.Minimum, out min);
                IPAddress.TryParse(Addresses.Maximum, out max);

                if (min != null && max != null)
                {
                    var minBytes = min.GetAddressBytes();
                    var maxBytes = max.GetAddressBytes();

                    var b = minBytes[3];
                    var e = maxBytes[3];

                    bool allow = true;

                    for (int i = b; i <= e; i++)
                    {
                        byte x = (byte)i;
                        var ip = new IPAddress(new byte[] { minBytes[0], minBytes[1], minBytes[2], x });

                        // Check if in Denied list
                        if (Addresses.DeniedAddresses != null) allow = !Addresses.DeniedAddresses.ToList().Exists(o => o.ToString() == ip.ToString());

                        // Check if already added to list
                        allow = allow && !l.Exists(o => o.ToString() == i.ToString());

                        if (allow) l.Add(ip);
                    }

                }

                return l.ToArray();
            }

            return null;
        }

        private void InitializePortRange()
        {
            int start = 5000;
            var size = 20;
            var portRange = new int[size];
            for (var i = 0; i < size; i++) portRange[i] = start++;
            PortRange = portRange;
        }

        private IPAddress GetHostAddress()
        {
            // Get All Network Interfaces
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            if (interfaces != null)
            {
                // Find Network Interface that matches the InterfaceId
                var obj = interfaces.ToList().Find(o => o.Id == InterfaceId);
                if (obj != null)
                {
                    var ips = obj.GetIPProperties();
                    if (ips != null && ips.UnicastAddresses != null)
                    {
                        // Find the Address for Internetwork communication
                        var ip = ips.UnicastAddresses.ToList().Find(o => o.Address != null && o.Address.AddressFamily == AddressFamily.InterNetwork);
                        if (ip != null) return ip.Address;
                    }
                }
            }

            return null;
        }

        private IPAddress[] GetDefaultAddresses()
        {
            var l = new List<IPAddress>();

            var host = GetHostAddress();
            if (host != null)
            {
                IPNetwork ip;
                if (IPNetwork.TryParse(host.ToString(), out ip))
                {
                    var addresses = IPNetwork.ListIPAddress(ip);
                    if (addresses != null)
                    {
                        foreach (var address in addresses) l.Add(address);
                    }
                }
            }

            return l.ToArray();
        }

        private static IPAddress[] GetIpAddressFromString(string[] strings)
        {
            var l = new List<IPAddress>();

            foreach (var s in strings)
            {
                IPAddress ip;
                if (IPAddress.TryParse(s, out ip)) l.Add(ip);
            }

            return l.ToArray();
        }


        #region "MAC Address"

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref int PhyAddrLen);

        /// <summary>
        /// Gets the MAC address (<see cref="PhysicalAddress"/>) associated with the specified IP.
        /// </summary>
        /// <param name="ipAddress">The remote IP address.</param>
        /// <returns>The remote machine's MAC address.</returns>
        private static PhysicalAddress GetMacAddress(IPAddress ipAddress)
        {
            const int MacAddressLength = 6;
            int length = MacAddressLength;
            var macBytes = new byte[MacAddressLength];
            if (SendARP(BitConverter.ToInt32(ipAddress.GetAddressBytes(), 0), 0, macBytes, ref length) == 0)
            {
                return new PhysicalAddress(macBytes);
            }

            return null;
        }

        #endregion
    }
}
