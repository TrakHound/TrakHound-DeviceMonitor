// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using MTConnect;
using MTConnect.Clients;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Xml.Serialization;
using Headers = MTConnect.Headers;
using MTConnectAssets = MTConnect.MTConnectAssets;
using MTConnectDevices = MTConnect.MTConnectDevices;
using MTConnectStreams = MTConnect.MTConnectStreams;
using TrakHound.Api.v2.Data;

namespace TrakHound.TempServer.MTConnect
{
    /// <summary>
    /// Handles MTConnect Agent connection data streams
    /// </summary>
    public class MTConnectConnection
    {
        private const int STATUS_UPDATE_INTERVAL = 10000; // 10 Seconds

        private static Logger log = LogManager.GetCurrentClassLogger();

        private object _lock = new object();
        private string agentUrl;

        private Timer statusUpdateTimer;
        private string availabilityId;
        private long currentAgentInstanceId;
        private bool previousAvailable;
        private bool previousConnected;
        private long previousAgentInstanceId;
        private bool first = true;


        private MTConnectClient _agentClient;
        /// <summary>
        /// Gets the underlying MTConnectClient object. Read Only.
        /// </summary>
        [XmlIgnore]
        public MTConnectClient AgentClient { get { return _agentClient; } }

        private string _deviceId;
        /// <summary>
        /// Gets the Device ID. Read Only.
        /// </summary>
        [XmlAttribute("deviceId")]
        [JsonProperty("deviceId")]
        public string DeviceId
        {
            get { return _deviceId; }
            set
            {
                if (_deviceId != null) throw new InvalidOperationException("Cannot set value. DeviceId is ReadOnly!");
                _deviceId = value;
            }
        }

        [XmlAttribute("enabled")]
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        private string _address;
        /// <summary>
        /// Gets the Address for the MTConnect Agent. Read Only.
        /// </summary>
        [XmlAttribute("address")]
        [JsonProperty("address")]
        public string Address
        {
            get { return _address; }
            set
            {
                if (_address != null) throw new InvalidOperationException("Cannot set value. Address is ReadOnly!");
                _address = value;
            }
        }

        private string _physicalAddress;
        /// <summary>
        /// Gets the Physical Address for the MTConnect Agent. Read Only.
        /// </summary>
        [XmlAttribute("physicalAddress")]
        [JsonProperty("physicalAddress")]
        public string PhysicalAddress
        {
            get { return _physicalAddress; }
            set
            {
                if (_physicalAddress != null) throw new InvalidOperationException("Cannot set value. PhysicalAddress is ReadOnly!");
                _physicalAddress = value;
            }
        }

        private int _port = -1;
        /// <summary>
        /// Gets the Port for the MTConnect Agent. Read Only.
        /// </summary>
        [XmlAttribute("port")]
        [JsonProperty("port")]
        public int Port
        {
            get { return _port; }
            set
            {
                if (_port >= 0) throw new InvalidOperationException("Cannot set value. Port is ReadOnly!");
                _port = value;
            }
        }

        private string _deviceName;
        /// <summary>
        /// Gets the Name of the MTConnect Device. Read Only.
        /// </summary>
        [XmlAttribute("deviceName")]
        [JsonProperty("deviceName")]
        public string DeviceName
        {
            get { return _deviceName; }
            set
            {
                if (_deviceName != null) throw new InvalidOperationException("Cannot set value. DeviceName is ReadOnly!");
                _deviceName = value;
            }
        }

        private int _interval = -1;
        /// <summary>
        /// Gets the Name of the MTConnect Device. Read Only.
        /// </summary>
        [XmlAttribute("interval")]
        [JsonProperty("interval")]
        public int Interval
        {
            get
            {
                if (_interval < 0) _interval = 500;
                return _interval;
            }
            set
            {
                if (_interval >= 0) throw new InvalidOperationException("Cannot set value. Interval is ReadOnly!");
                if (value < 0) throw new ArgumentOutOfRangeException("Interval", "Interval must be greater than zero!");
                _interval = value;
            }
        }


        /// <summary>
        /// Event raised when a new Agent is read.
        /// </summary>
        internal event AgentHandler AgentReceived;

        /// <summary>
        /// Event raised when new Assets are read.
        /// </summary>
        internal event AssetsHandler AssetsReceived;

        /// <summary>
        /// Event raised when a new Device is read.
        /// </summary>
        internal event DeviceHandler DeviceReceived;

        /// <summary>
        /// Event raised when new Components are read.
        /// </summary>
        internal event ComponentsHandler ComponentsReceived;

        /// <summary>
        /// Event raised when new DataItems are read.
        /// </summary>
        internal event DataItemsHandler DataItemsReceived;

        /// <summary>
        /// Event raised when new StreamData is read.
        /// </summary>
        internal event SampleHandler SamplesReceived;

        /// <summary>
        /// Event raised when the Status is updated.
        /// </summary>
        internal event StatusHandler StatusUpdated;


        public MTConnectConnection()
        {
            Enabled = true;
        }

        public MTConnectConnection(string deviceId, string address, int port, string physicalAddress, string deviceName)
        {
            Init(deviceId, address, port, physicalAddress, deviceName, 100);
        }

        public MTConnectConnection(string deviceId, string address, int port, string physicalAddress, string deviceName, int interval)
        {
            Init(deviceId, address, port, physicalAddress, deviceName, interval);
        }

        private void Init(string deviceId, string address, int port, string physicalAddress, string deviceName, int interval)
        {
            Enabled = true;
            _deviceId = deviceId;
            _address = address;
            _physicalAddress = physicalAddress;
            _port = port;
            _deviceName = deviceName;
            _interval = interval;
        }

        /// <summary>
        /// Start the Device and begin reading the MTConnect Data.
        /// </summary>
        public void Start()
        {
            // Initialize Status
            UpdateStatus(false, false, -1);

            // Start Status Update Timer
            statusUpdateTimer = new Timer();
            statusUpdateTimer.Interval = STATUS_UPDATE_INTERVAL;
            statusUpdateTimer.Elapsed += StatusUpdateTimer_Elapsed;
            statusUpdateTimer.Start();

            // Start MTConnect Agent Client
            StartAgentClient();
        }

        /// <summary>
        /// Stop the Device
        /// </summary>
        public void Stop()
        {
            if (statusUpdateTimer != null) statusUpdateTimer.Stop();

            if (_agentClient != null) _agentClient.Stop();
        }

        private void StartAgentClient()
        {
            if (!string.IsNullOrEmpty(Address))
            {
                // Normalize Properties
                _address = _address.Replace("http://", "");
                if (_port < 0) _port = 5000;
                if (_interval < 0) _interval = 500;

                // Create the MTConnect Agent Base URL
                agentUrl = string.Format("http://{0}:{1}", _address, _port);

                // Create a new MTConnectClient using the baseUrl
                _agentClient = new MTConnectClient(agentUrl, _deviceName);
                _agentClient.Interval = _interval;

                // Subscribe to the Event handlers to receive status events
                _agentClient.Started += _agentClient_Started;
                _agentClient.Stopped += _agentClient_Stopped;

                // Subscribe to the Event handlers to receive the MTConnect documents
                _agentClient.ProbeReceived += DevicesDocumentReceived;
                _agentClient.CurrentReceived += StreamsDocumentReceived;
                _agentClient.SampleReceived += StreamsDocumentReceived;
                _agentClient.AssetsReceived += AssetsDocumentReceived;
                _agentClient.ConnectionError += _agentClient_ConnectionError;

                // Start the MTConnectClient
                _agentClient.Start();
            }
            else
            {
                log.Warn("MTConnect Address Invalid : " + _deviceId + " : " + _address);
            }
        }

        private void _agentClient_ConnectionError(Exception ex)
        {
            log.Info("Error Connecting to MTConnect Agent @ " + _agentClient.BaseUrl);
            log.Trace(ex);

            UpdateConnectedStatus(false);
        }

        private void _agentClient_Started()
        {
            log.Debug("MTConnect Client Started : " + agentUrl + "/" + _deviceName);
        }

        private void _agentClient_Stopped()
        {
            log.Debug("MTConnect Client Stopped : " + agentUrl + "/" + _deviceName);
        }


        private void DevicesDocumentReceived(MTConnectDevices.Document document)
        {
            log.Trace("MTConnect Devices Document Received @ " + DateTime.Now.ToString("o"));

            UpdateConnectedStatus(true);

            if (document.Header != null && document.Devices != null && document.Devices.Count == 1)
            {
                currentAgentInstanceId = document.Header.InstanceId;
                DateTime timestamp = document.Header.CreationTime;

                // Send Agent
                AgentReceived?.Invoke(this, Create(_deviceId, document.Header));

                var dataItems = new List<DataItemDefinition>();
                var components = new List<ComponentDefinition>();

                var device = document.Devices[0];

                // Send Device
                DeviceReceived?.Invoke(this, Create(_deviceId, currentAgentInstanceId, device));

                // Add DataItems
                foreach (var item in device.DataItems)
                {
                    dataItems.Add(Create(_deviceId, currentAgentInstanceId, device.Id, "Device", item));
                }

                // Add Component 
                components.AddRange(GetComponents(_deviceId, currentAgentInstanceId, device.Id, "Device", device.Components));

                // Add Component DataItems
                foreach (var component in device.GetComponents())
                {
                    foreach (var dataItem in component.DataItems)
                    {
                        dataItems.Add(Create(_deviceId, currentAgentInstanceId, component.Id, "Component", dataItem));
                    }
                }

                // Get the Availability DataItem
                var avail = device.DataItems.Find(o => o.Type == "AVAILABILITY");
                if (avail != null) availabilityId = avail.Id;

                // Send ContainerDefinition Objects
                if (components.Count > 0) ComponentsReceived?.Invoke(this, components);

                // Send DataItemDefinition Objects
                if (dataItems.Count > 0) DataItemsReceived?.Invoke(this, dataItems);
            }
        }

        private void StreamsDocumentReceived(MTConnectStreams.Document document)
        {
            log.Trace("MTConnect Streams Document Received @ " + DateTime.Now.ToString("o"));

            UpdateConnectedStatus(true);

            if (!document.DeviceStreams.IsNullOrEmpty())
            {
                var samples = new List<TrakHound.Api.v2.Data.Sample>();

                var deviceStream = document.DeviceStreams[0];

                foreach (var dataItem in deviceStream.DataItems)
                {
                    samples.Add(Create(_deviceId, document.Header.InstanceId, dataItem));
                }

                // Get Availability
                if (!string.IsNullOrEmpty(availabilityId))
                {
                    var avail = deviceStream.DataItems.Find(o => o.DataItemId == availabilityId);
                    if (avail != null) UpdateAvailableStatus(avail.CDATA == "AVAILABLE");
                }            

                if (samples.Count > 0) SamplesReceived?.Invoke(this, samples);
            }
        }

        private void AssetsDocumentReceived(MTConnectAssets.Document document)
        {
            log.Trace("MTConnect Assets Document Received @ " + DateTime.Now.ToString("o"));

            UpdateConnectedStatus(true);

            if (document.Assets != null)
            {
                var assets = new List<AssetDefinition>();

                foreach (var asset in document.Assets.Assets)
                {
                    assets.Add(Create(_deviceId, document.Header.InstanceId, asset));
                }

                if (assets.Count > 0) AssetsReceived?.Invoke(this, assets);
            }
        }


        private static List<ComponentDefinition> GetComponents(string deviceId, long agentInstanceId, string parentId, string parentType, MTConnectDevices.ComponentCollection components)
        {
            var l = new List<ComponentDefinition>();

            foreach (var component in components.Components)
            {
                l.Add(Create(deviceId, agentInstanceId, parentId, parentType, component));
                l.AddRange(GetComponents(deviceId, agentInstanceId, component.Id, "Component", component.SubComponents));
            }

            return l;
        }


        private static AgentDefinition Create(string deviceId, Headers.MTConnectDevicesHeader header)
        {
            var obj = new AgentDefinition();

            // TrakHound Properties
            obj.DeviceId = deviceId;
            obj.Timestamp = header.CreationTime;

            // MTConnect Properties
            obj.InstanceId = header.InstanceId;
            obj.Sender = header.Sender;
            obj.Version = header.Version;
            obj.BufferSize = header.BufferSize;
            obj.TestIndicator = header.TestIndicator;

            return obj;
        }

        private static AssetDefinition Create(string deviceId, long agentInstanceId, MTConnectAssets.Asset asset)
        {
            var obj = new AssetDefinition();

            // TrakHound Properties
            obj.DeviceId = deviceId;
            obj.AgentInstanceId = agentInstanceId;
            obj.Id = asset.AssetId;
            obj.Timestamp = asset.Timestamp;
            obj.Type = asset.Type;
            obj.Xml = asset.Xml;

            return obj;
        }

        private static DeviceDefinition Create(string deviceId, long agentInstanceId, MTConnectDevices.Device device)
        {
            var obj = new DeviceDefinition();

            obj.DeviceId = deviceId;

            // MTConnect Properties
            obj.AgentInstanceId = agentInstanceId;
            obj.Id = device.Id;
            obj.Uuid = device.Uuid;
            obj.Name = device.Name;
            obj.NativeName = device.NativeName;
            obj.SampleInterval = device.SampleInterval;
            obj.SampleRate = device.SampleRate;
            obj.Iso841Class = device.Iso841Class;
            if (device.Description != null)
            {
                // Check if pointing to the MTConnect Demo at http://agent.mtconnect.org
                if (device.Description.Manufacturer == "SystemInsights") obj.Description = "MTConnect Demo";
                else
                {
                    obj.Manufacturer = device.Description.Manufacturer;
                    obj.Description = device.Description.CDATA;
                }

                obj.Model = device.Description.Model;
                obj.SerialNumber = device.Description.SerialNumber;
                obj.Station = device.Description.Station;
            }

            return obj;
        }

        private static ComponentDefinition Create(string deviceId, long agentInstanceId, string parentId, string parentType, MTConnectDevices.Component component)
        {
            var obj = new ComponentDefinition();

            // TrakHound Properties
            obj.DeviceId = deviceId;
            obj.ParentId = parentId;
            obj.ParentType = parentType;

            // MTConnect Properties
            obj.AgentInstanceId = agentInstanceId;
            obj.Type = component.Type;
            obj.Id = component.Id;
            obj.Uuid = component.Uuid;
            obj.Name = component.Name;
            obj.NativeName = component.NativeName;
            obj.SampleInterval = component.SampleInterval;
            obj.SampleRate = component.SampleRate;

            return obj;
        }

        private static DataItemDefinition Create(string deviceId, long agentInstanceId, string parentId, string parentType, MTConnectDevices.DataItem dataItem)
        {
            var obj = new DataItemDefinition();

            // TrakHound Properties
            obj.DeviceId = deviceId;
            obj.ParentId = parentId;
            obj.ParentType = parentType;

            // MTConnect Properties
            obj.AgentInstanceId = agentInstanceId;
            obj.Id = dataItem.Id;
            obj.Name = dataItem.Name;
            obj.Category = dataItem.Category.ToString();
            obj.Type = dataItem.Type;
            obj.SubType = dataItem.SubType;
            obj.Statistic = dataItem.Statistic;
            obj.Units = dataItem.Units;
            obj.NativeUnits = dataItem.NativeUnits;
            obj.NativeScale = dataItem.NativeScale;
            obj.CoordinateSystem = dataItem.CoordinateSystem;
            obj.SampleRate = dataItem.SampleRate;
            obj.Representation = dataItem.Representation;
            obj.SignificantDigits = dataItem.SignificantDigits;

            return obj;
        }

        private static TrakHound.Api.v2.Data.Sample Create(string deviceId, long agentInstanceId, MTConnectStreams.DataItem dataItem)
        {
            var obj = new TrakHound.Api.v2.Data.Sample();

            obj.DeviceId = deviceId;

            obj.Id = dataItem.DataItemId;
            obj.AgentInstanceId = agentInstanceId;
            obj.Sequence = dataItem.Sequence;
            obj.Timestamp = dataItem.Timestamp;
            obj.CDATA = dataItem.CDATA;
            if (dataItem.Category == DataItemCategory.CONDITION) obj.Condition = ((MTConnectStreams.Condition)dataItem).ConditionValue.ToString();

            return obj;
        }

        private static Status Create(string deviceId, bool connected, bool available, long agentInstanceId)
        {
            var obj = new Status();
            obj.DeviceId = deviceId;
            obj.Timestamp = DateTime.UtcNow;
            obj.Connected = connected;
            obj.Available = available;
            obj.AgentInstanceId = agentInstanceId;

            return obj;
        }


        private void UpdateAvailableStatus(bool available)
        {
            bool changed = false;
            bool connected = false;

            lock (_lock)
            {
                connected = previousConnected;
                changed = available != previousAvailable || connected != previousConnected;
                previousAvailable = available;
            }

            if (changed || first) UpdateStatus(connected, available, currentAgentInstanceId);

            first = false;
        }

        private void UpdateConnectedStatus(bool connected)
        {
            bool changed = false;
            bool available = false;

            lock (_lock)
            {
                available = previousAvailable;
                changed = available != previousAvailable || connected != previousConnected;
                previousConnected = connected;
            }

            if (changed || first) UpdateStatus(connected, available, currentAgentInstanceId);

            first = false;
        }

        private void UpdateStatus(bool connected, bool available, long agentInstanceId)
        {
            log.Debug("Status Updated : " + _deviceId + " : Connected=" + connected + " : Available=" + available);
            StatusUpdated?.Invoke(this, Create(_deviceId, connected, available, agentInstanceId));
        }

        private void StatusUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lock) UpdateStatus(previousConnected, previousAvailable, currentAgentInstanceId);
        }
    }
}
