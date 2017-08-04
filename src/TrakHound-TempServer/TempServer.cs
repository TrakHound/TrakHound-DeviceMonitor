// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Events;
using TrakHound.Api.v2.Data;
using System.Threading;
using System.IO;

namespace TrakHound.TempServer
{
    /// <summary>
    /// Handles all functions for collecting data from MTConnect Agents and sending that data to Predix
    /// </summary>
    public class Server
    {
        private const int TIMESPAN = 86400 * 1000; // 24 Hours in Milliseconds
        //private const int TIMESPAN = 150 * 1000; // 2.5 Minutes in Milliseconds

        private static Logger log = LogManager.GetCurrentClassLogger();

        internal static object _lock = new object();
        private int devicesFound = 0;
        private MTConnect.DeviceFinder.MTConnectDevice foundDevice;
        private MTConnect.MTConnectConnectionStartQueue connectionStartQueue = new MTConnect.MTConnectConnectionStartQueue();
        private List<MTConnect.DeviceFinder.MTConnectDeviceFinder> deviceFinders = new List<MTConnect.DeviceFinder.MTConnectDeviceFinder>();

        private System.Timers.Timer backupTimer;
        private DateTime lastBackupTime;

        internal static List<ConnectionDefinition> storedConnections = new List<ConnectionDefinition>();
        internal static List<AgentDefinition> storedAgents = new List<AgentDefinition>();
        internal static List<DeviceDefinition> storedDevices = new List<DeviceDefinition>();
        internal static List<ComponentDefinition> storedComponents = new List<ComponentDefinition>();
        internal static List<DataItemDefinition> storedDataItems = new List<DataItemDefinition>();
        internal static List<Sample> currentSamples = new List<Sample>();
        internal static List<Sample> storedSamples = new List<Sample>();
        internal static List<Status> storedStatus = new List<Status>();

        class EventDataItems
        {
            public string DeviceId { get; set; }

            public string Version { get; set; }

            public List<string> DataItemIds { get; set; }

            public EventDataItems(string deviceId, string version)
            {
                DeviceId = deviceId;
                Version = version;
                DataItemIds = new List<string>();
            }
        }

        private List<EventDataItems> eventDataItems = new List<EventDataItems>();


        private Configuration _configuration;
        /// <summary>
        /// Gets the Configuration that was used to create the DataClient. Read Only.
        /// </summary>
        public Configuration Configuration { get { return _configuration; } }


        public Server(Configuration config)
        {
            _configuration = config;

            Database.Initialize();

            ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
            {
                object obj = Database.ReadConnections();
                if (obj != null)
                {
                    var connections = (List<ConnectionDefinition>)obj;
                    foreach (var connection in connections)
                    {
                        lock (_lock)
                        {
                            if (!storedConnections.Exists(x => x.DeviceId == connection.DeviceId))
                            {
                                storedConnections.Add(connection);
                            }
                        }
                    }
                }

                obj = Database.ReadAgents();
                if (obj != null)
                {
                    var agents = (List<AgentDefinition>)obj;
                    foreach (var agent in agents)
                    {
                        lock (_lock)
                        {
                            if (!storedAgents.Exists(x => x.DeviceId == agent.DeviceId))
                            {
                                storedAgents.Add(agent);
                            }
                        }
                    }
                }

                obj = Database.ReadDevices();
                if (obj != null)
                {
                    var devices = (List<DeviceDefinition>)obj;
                    foreach (var device in devices)
                    {
                        lock (_lock)
                        {
                            if (!storedDevices.Exists(x => x.DeviceId == device.DeviceId))
                            {
                                storedDevices.Add(device);
                            }
                        }
                    }
                }

                obj = Database.ReadComponents();
                if (obj != null)
                {
                    var components = (List<ComponentDefinition>)obj;
                    foreach (var component in components)
                    {
                        lock (_lock)
                        {
                            if (!storedComponents.Exists(x => x.DeviceId == component.DeviceId && x.Id == component.Id))
                            {
                                storedComponents.Add(component);
                            }
                        }
                    }
                }

                obj = Database.ReadDataItems();
                if (obj != null)
                {
                    var dataItems = (List<DataItemDefinition>)obj;
                    foreach (var dataItem in dataItems)
                    {
                        lock (_lock)
                        {
                            if (!storedDataItems.Exists(x => x.DeviceId == dataItem.DeviceId && x.Id == dataItem.Id))
                            {
                                storedDataItems.Add(dataItem);
                            }
                        }
                    }
                }

                obj = Database.ReadSamples();
                if (obj != null)
                {
                    var samples = (List<Sample>)obj;
                    foreach (var sample in samples)
                    {
                        lock (_lock)
                        {
                            if (!storedSamples.Exists(x => x.DeviceId == sample.DeviceId && x.Id == sample.Id && x.Timestamp == sample.Timestamp))
                            {
                                storedSamples.Add(sample);
                            }
                        }
                    }
                }
            }));

            connectionStartQueue.ConnectionStarted += ConnectionStartQueue_ConnectionStarted;
        }

        public void Start()
        {
            log.Info("TrakHound Temp Server Starting..");

            // Start Devices
            StartMTConnectDevices();

            // Start DeviceFinders
            StartMTConnectDeviceFinders();

            if (backupTimer != null) backupTimer.Stop();
            backupTimer = new System.Timers.Timer();
            backupTimer.Interval = 60000;
            backupTimer.Elapsed += BackupTimer_Elapsed;
            backupTimer.Start();
        }

        public void Stop()
        {
            log.Info("TrakHound Temp Server Stopping..");

            // Stop Devices
            foreach (var device in _configuration.Devices) device.Stop();

            // Stop the Device Finders
            foreach (var deviceFinder in deviceFinders) deviceFinder.Stop();
        }

        internal static List<ConnectionDefinition> ReadConnections()
        {
            lock (_lock)
            {
                var connections = new List<ConnectionDefinition>();

                foreach (var connection in storedConnections)
                {
                    var status = storedStatus.Find(o => o.DeviceId == connection.DeviceId);
                    if (status != null && status.Connected) connections.Add(connection);
                }

                return connections;
            }
        }

        internal static ConnectionDefinition ReadConnection(string deviceId)
        {
            lock(_lock)
            {
                return storedConnections.Find(o => o.DeviceId == deviceId);
            }
        }

        internal static AgentDefinition ReadAgent(string deviceId)
        {
            lock (_lock)
            {
                return storedAgents.Find(o => o.DeviceId == deviceId);
            }
        }

        internal static DeviceDefinition ReadDevice(string deviceId)
        {
            lock (_lock)
            {
                return storedDevices.Find(o => o.DeviceId == deviceId);
            }
        }

        internal static List<ComponentDefinition> ReadComponents(string deviceId)
        {
            lock (_lock)
            {
                return storedComponents.FindAll(o => o.DeviceId == deviceId);
            }
        }

        internal static List<DataItemDefinition> ReadDataItems(string deviceId)
        {
            lock (_lock)
            {
                return storedDataItems.FindAll(o => o.DeviceId == deviceId);
            }
        }

        internal static Status ReadStatus(string deviceId)
        {
            lock (_lock)
            {
                return storedStatus.Find(o => o.DeviceId == deviceId);
            }
        }

        internal static List<Sample> ReadSamples(string deviceId, DateTime from, DateTime to)
        {
            List<string> ids = null;
            var sortedSamples = new List<Sample>();

            lock (_lock)
            {
                ids = storedDataItems.FindAll(o => o.DeviceId == deviceId).Select(o => o.Id).ToList();
                sortedSamples.AddRange(currentSamples);
                sortedSamples.AddRange(storedSamples);
                sortedSamples = sortedSamples.OrderBy(o => o.Timestamp).ToList();
            }

            if (!ids.IsNullOrEmpty() && !sortedSamples.IsNullOrEmpty())
            {
                var samples = new List<Sample>();

                foreach (var id in ids)
                {
                    var initialSample = sortedSamples.FindLast(o => o.DeviceId == deviceId && o.Id == id);
                    if (from > DateTime.MinValue)
                    {
                        var sample = sortedSamples.Find(o => o.DeviceId == deviceId && o.Id == id && o.Timestamp >= from);
                        if (sample != null) initialSample = sample;
                    }
                    if (to > DateTime.MinValue && initialSample.Timestamp > to) initialSample = sortedSamples.FindLast(o => o.DeviceId == deviceId && o.Id == id && o.Timestamp <= to);

                    if (initialSample != null) samples.Add(initialSample);

                    if (from > DateTime.MinValue) samples.AddRange(sortedSamples.FindAll(o => o.DeviceId == deviceId && o.Id == id && o.Timestamp > from));
                    else if (to > DateTime.MinValue) samples.AddRange(sortedSamples.FindAll(o => o.DeviceId == deviceId && o.Id == id && o.Timestamp > from && o.Timestamp <= to));
                }

                return samples;
            }

            return null;
        }


        private void StartMTConnectDevices()
        {
            connectionStartQueue.Start();
            if (_configuration.Devices.Count > 0)
            {
                foreach (var device in _configuration.Devices)
                {
                    log.Info("Device Read : " + device.DeviceId + " : " + device.DeviceName + " : " + device.Address + " : " + device.Port);
                    StartMTConnectConnection(device);
                }
            }
            else
            {
                log.Info("No Devices in Configuration File");
            }
        }

        private void StartMTConnectDeviceFinders()
        {
            if (_configuration.DeviceFinderEnabled)
            {
                var deviceFinders = new List<MTConnect.DeviceFinder.MTConnectDeviceFinder>();

                if (_configuration.DeviceFinders == null || _configuration.DeviceFinders.Count < 1)
                {
                    // Find All NetworkInterfaces
                    var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    if (interfaces != null)
                    {
                        foreach (var ni in interfaces)
                        {
                            if (ni.OperationalStatus == OperationalStatus.Up && (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                            {
                                var deviceFinder = new MTConnect.DeviceFinder.MTConnectDeviceFinder(ni.Id, ni.Description);
                                deviceFinders.Add(deviceFinder);
                                _configuration.DeviceFinders.Add(deviceFinder);
                            }
                        }

                        _configuration.Save();
                    }
                }
                else
                {
                    // Add pre configured DeviceFinders that were read from the configuration file
                    foreach (var deviceFinder in _configuration.DeviceFinders) deviceFinders.Add(deviceFinder);
                }

                foreach (var deviceFinder in deviceFinders)
                {
                    deviceFinder.DeviceFound += DeviceFinder_DeviceFound;
                    deviceFinder.SearchCompleted += DeviceFinder_SearchCompleted;
                    deviceFinder.Start();
                }
            }
        }


        private void DeviceFinder_SearchCompleted(MTConnect.DeviceFinder.MTConnectDeviceFinder deviceFinder, long milliseconds)
        {
            if (devicesFound > 0) Configuration.Save();

            var time = TimeSpan.FromMilliseconds(milliseconds);
            log.Info(string.Format("Device Finder : Search Completed on {0} : {1} Devices Found in {2}", deviceFinder.InterfaceDescription, devicesFound, time.ToString()));

            devicesFound = 0;
            foundDevice = null;
        }

        private void DeviceFinder_DeviceFound(MTConnect.DeviceFinder.MTConnectDeviceFinder deviceFinder, MTConnect.DeviceFinder.MTConnectDevice device)
        {
            foundDevice = device;
            if (AddConnection(device)) devicesFound++;        
        }


        private bool AddConnection(MTConnect.DeviceFinder.MTConnectDevice device)
        {
            // Generate the Device ID Hash
            string deviceId = GenerateDeviceId(device);

            // Check to make sure the Device is not already added
            if (!Configuration.Devices.Exists(o => o.DeviceId == deviceId))
            {
                // Create a new MTConnect Connection and start it
                var d = new MTConnect.MTConnectConnection(deviceId, device.IpAddress.ToString(), device.Port, device.MacAddress.ToString(), device.DeviceName);
                Configuration.Devices.Add(d);
                StartMTConnectConnection(d);

                log.Info("New Connection Added : " + deviceId + " : " + device.DeviceName + " : " + device.IpAddress + " : " + device.Port);

                return true;
            }

            return false;
        }

        private void StartMTConnectConnection(MTConnect.MTConnectConnection connection)
        {
            if (connection.Enabled)
            {
                connection.AgentReceived += AgentReceived;
                connection.DeviceReceived += DeviceReceived;
                connection.ComponentsReceived += ComponentsReceived;
                connection.DataItemsReceived += DataItemsReceived;
                connection.SamplesReceived += SamplesReceived;
                connection.AssetsReceived += AssetsReceived;
                connection.StatusUpdated += StatusUpdated;

                lock (_lock)
                {
                    if (!storedConnections.Exists(o => o.DeviceId == connection.DeviceId))
                    {
                        var connectionDefinition = new ConnectionDefinition();
                        connectionDefinition.DeviceId = connection.DeviceId;
                        connectionDefinition.Address = connection.Address;
                        connectionDefinition.Port = connection.Port;
                        connectionDefinition.PhysicalAddress = connection.PhysicalAddress;
                        storedConnections.Add(connectionDefinition);
                    }
                }

                // Add to Start Queue (to prevent all Connections from starting at once and using too many resources)
                connectionStartQueue.Add(connection);
            }
        }

        private void ConnectionStartQueue_ConnectionStarted(MTConnect.MTConnectConnection connection)
        {
            log.Info("MTConnect Connection Started : " + connection.DeviceId + " : " + connection.DeviceName + " : " + connection.Address + " : " + connection.Port);
        }


        private void AgentReceived(MTConnect.MTConnectConnection connection, AgentDefinition agent)
        {
            eventDataItems.Add(new EventDataItems(agent.DeviceId, agent.Version));
            
            lock (_lock)
            {
                var i = storedAgents.FindIndex(o => o.DeviceId == agent.DeviceId);
                if (i >= 0) storedAgents.RemoveAt(i);
                storedAgents.Add(agent);
            }
        }

        private void DeviceReceived(MTConnect.MTConnectConnection connection, DeviceDefinition device)
        {
            lock (_lock)
            {
                var i = storedDevices.FindIndex(o => o.DeviceId == device.DeviceId);
                if (i >= 0) storedDevices.RemoveAt(i);
                storedDevices.Add(device);
            }
        }

        private void UpdateEventDataItems(string deviceId)
        {
            List<DataItemDefinition> dataItems = null;
            List<ComponentDefinition> components = null;

            lock (_lock)
            {
                dataItems = storedDataItems.FindAll(o => o.DeviceId == deviceId);
                components = storedComponents.FindAll(o => o.DeviceId == deviceId);
            }

            var eventItems = eventDataItems.Find(o => o.DeviceId == deviceId);
            if (eventItems != null)
            {
                var events = GetEvents(eventItems.Version);
                if (events != null)
                {
                    foreach (var e in events)
                    {
                        var eventIds = GetEventIds(e, dataItems, components);
                        if (!eventIds.IsNullOrEmpty())
                        {
                            foreach (var id in eventIds)
                            {
                                if (!eventItems.DataItemIds.Exists(o => o == id))
                                {
                                    eventItems.DataItemIds.Add(id);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ComponentsReceived(MTConnect.MTConnectConnection connection, List<ComponentDefinition> components)
        {
            foreach (var component in components)
            {
                lock (_lock)
                {
                    var i = storedComponents.FindIndex(o => o.DeviceId == component.DeviceId && o.Id == component.Id);
                    if (i >= 0) storedComponents.RemoveAt(i);
                    storedComponents.Add(component);
                }
            }

            UpdateEventDataItems(connection.DeviceId);
        }

        private void DataItemsReceived(MTConnect.MTConnectConnection connection, List<DataItemDefinition> dataItems)
        {
            foreach (var dataItem in dataItems)
            {
                lock (_lock)
                {
                    var i = storedDataItems.FindIndex(o => o.DeviceId == dataItem.DeviceId && o.Id == dataItem.Id);
                    if (i >= 0) storedDataItems.RemoveAt(i);
                    storedDataItems.Add(dataItem);
                }
            }

            UpdateEventDataItems(connection.DeviceId);
        }
   
        private void SamplesReceived(MTConnect.MTConnectConnection connection, List<Sample> samples)
        {
            var eventItems = eventDataItems.Find(o => o.DeviceId == connection.DeviceId);
            if (eventItems != null)
            {
                var eventSamples = samples.FindAll(o => o.DeviceId == connection.DeviceId && eventItems.DataItemIds.Exists(y => y == o.Id));
                if (!eventSamples.IsNullOrEmpty())
                {
                    storedSamples.AddRange(eventSamples);
                }

                var ids = samples.Select(o => o.Id).Distinct();
                foreach (var id in ids)
                {
                    lock(_lock)
                    {
                        var i = currentSamples.FindIndex(o => o.DeviceId == connection.DeviceId && o.Id == id);
                        if (i >= 0) currentSamples.RemoveAt(i);
                        currentSamples.Add(samples.Find(o => o.DeviceId == connection.DeviceId && o.Id == id));
                    }
                }
            }
        }

        private void AssetsReceived(MTConnect.MTConnectConnection connection, List<AssetDefinition> assets)
        {

        }

        private void StatusUpdated(MTConnect.MTConnectConnection connection, Status status)
        {
            lock (_lock)
            {
                var i = storedStatus.FindIndex(o => o.DeviceId == status.DeviceId);
                if (i >= 0) storedStatus.RemoveAt(i);
                storedStatus.Add(status);
            }
        }


        private void RunBackup()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
            {
                if (!storedConnections.IsNullOrEmpty()) lock (_lock) Database.Write(storedConnections);
                if (!storedAgents.IsNullOrEmpty()) lock (_lock) Database.Write(storedAgents);
                if (!storedDevices.IsNullOrEmpty()) lock (_lock) Database.Write(storedDevices);
                if (!storedComponents.IsNullOrEmpty()) lock (_lock) Database.Write(storedComponents);
                if (!storedDataItems.IsNullOrEmpty()) lock (_lock) Database.Write(storedDataItems);
                if (!storedSamples.IsNullOrEmpty()) lock (_lock) Database.Write(storedSamples.FindAll(x => x.Timestamp > lastBackupTime));

                List<DeviceDefinition> safeDevices = null;
                List<Sample> safeSamples = null;
                lock (_lock)
                {
                    safeDevices = storedDevices;
                    safeSamples = storedSamples;
                }
                if (!safeDevices.IsNullOrEmpty() && !safeSamples.IsNullOrEmpty())
                {
                    // Only delete samples that have been updated within the TIMESPAN
                    foreach (var deviceId in safeDevices.Select(x => x.DeviceId))
                    {
                        var ids = safeSamples.FindAll(x => x.DeviceId == deviceId).Select(x => x.Id).Distinct();
                        foreach (var id in ids)
                        {
                            // Find the last sample within before the TIMESPAN
                            var expiredSamples = safeSamples.FindAll(x => x.DeviceId == deviceId && x.Id == id && x.Timestamp < DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds(TIMESPAN)));
                            if (!expiredSamples.IsNullOrEmpty() && expiredSamples.Count > 1)
                            {
                                var lastSample = expiredSamples.OrderBy(x => x.Timestamp).Last();

                                // Delete all but the last Sample found before the TIMESPAN
                                string QUERY_FORMAT = "DELETE FROM `samples` WHERE `id`='{0}' AND `timestamp` < {1}";
                                string query = string.Format(QUERY_FORMAT, id, lastSample.Timestamp.ToUnixTime());
                                Console.WriteLine(query);
                                Database.ExecuteQuery(query);

                                // Remove from Memory cache
                                lock (_lock) safeSamples.RemoveAll(x => x.Timestamp < lastSample.Timestamp);
                            }
                        }
                    }
                }

                lastBackupTime = DateTime.UtcNow;
            }));
        }

        private void BackupTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            RunBackup();
        }


        private static List<Event> GetEvents(string agentVersion)
        {
            var version = new Version(agentVersion);
            var version13 = new Version("1.3.0");
            var version12 = new Version("1.2.0");
            var version11 = new Version("1.1.0");
            var version10 = new Version("1.0.0");

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "v12events.config");

            if (version >= version13)
            {
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "v13events.config");
            }

            // Read the EventsConfiguration file
            var config = EventsConfiguration.Get(configPath);
            if (config != null)
            {
                return config.Events;
            }

            return null;
        }

        private static string[] GetEventIds(Event e, List<DataItemDefinition> dataItems, List<ComponentDefinition> components)
        {
            var ids = new List<string>();

            foreach (var response in e.Responses)
            {
                foreach (var trigger in response.Triggers.OfType<Trigger>())
                {
                    foreach (var id in GetFilterIds(trigger.Filter, dataItems, components))
                    {
                        if (!ids.Exists(o => o == id)) ids.Add(id);
                    }
                }

                foreach (var multiTrigger in response.Triggers.OfType<MultiTrigger>())
                {
                    foreach (var trigger in multiTrigger.Triggers)
                    {
                        foreach (var id in GetFilterIds(trigger.Filter, dataItems, components))
                        {
                            if (!ids.Exists(o => o == id)) ids.Add(id);
                        }
                    }
                }
            }

            return ids.ToArray();
        }

        private static string[] GetFilterIds(string filter, List<DataItemDefinition> dataItems, List<ComponentDefinition> components)
        {
            var ids = new List<string>();

            foreach (var dataItem in dataItems)
            {
                var dataFilter = new DataFilter(filter, dataItem, components);
                if (dataFilter.IsMatch() && !ids.Exists(o => o == dataItem.Id))
                {
                    ids.Add(dataItem.Id);
                }
            }

            return ids.ToArray();
        }


        public static string GenerateDeviceId(MTConnect.DeviceFinder.MTConnectDevice device)
        {
            // Create Identifier input
            string s = string.Format("{0}|{1}|{2}", device.DeviceName, device.Port, device.MacAddress);
            s = Uri.EscapeDataString(s);

            // Create Hash
            var b = Encoding.UTF8.GetBytes(s);
            var h = SHA1.Create();
            b = h.ComputeHash(b);
            var l = b.ToList();
            l.Reverse();
            b = l.ToArray();

            // Convert to Base64 string
            s = Convert.ToBase64String(b);

            // Remove non alphanumeric characters
            var regex = new Regex("[^a-zA-Z0-9 -]");
            s = regex.Replace(s, "");
            s = s.ToUpper();

            return s;
        }
    }
}
