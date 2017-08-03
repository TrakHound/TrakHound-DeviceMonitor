// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using DeviceFinder = TrakHound.TempServer.MTConnect.DeviceFinder;

namespace TrakHound.TempServer
{
    /// <summary>
    /// The configuration for the Predix Server
    /// </summary>
    [XmlRoot("TempServer")]
    public class Configuration
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public static Configuration Current { get; set; }

        /// <summary>
        /// The default filename used for the Configuration file
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public const string FILENAME = "temp-server.config";

        [XmlIgnore]
        [JsonIgnore]
        public const string DEFAULT_FILENAME = "temp-server.config.default";

        protected string _path;
        /// <summary>
        /// Gets the file path that the Configuration was read from. Read Only.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public string Path { get { return _path; } }


        [XmlAttribute("deviceFinderEnabled")]
        [JsonProperty("deviceFinderEnabled")]
        public bool DeviceFinderEnabled { get; set; }

        /// <summary>
        /// Gets or Sets the DeviceFinder to use for finding new MTConnect devices on the network
        /// </summary>
        [XmlArray("DeviceFinders")]
        [XmlArrayItem("DeviceFinder", typeof(DeviceFinder.MTConnectDeviceFinder))]
        [JsonProperty("deviceFinders")]
        public List<DeviceFinder.MTConnectDeviceFinder> DeviceFinders { get; set; }

        /// <summary>
        /// Gets or Sets a list of Devices that read from the MTConnect Agent.
        /// </summary>
        [XmlArray("Devices")]
        [XmlArrayItem("Device", typeof(MTConnect.MTConnectConnection))]
        [JsonProperty("devices")]
        public List<MTConnect.MTConnectConnection> Devices { get; set; }

        [XmlArray("Prefixes")]
        [XmlArrayItem("Prefix")]
        public List<string> Prefixes { get; set; }

        [XmlAttribute("backupInterval")]
        public int BackupInterval { get; set; }


        public Configuration()
        {
            DeviceFinderEnabled = true;
            Devices = new List<MTConnect.MTConnectConnection>();
        }

        /// <summary>
        /// Reads a new Configuration from a file path.
        /// </summary>
        /// <param name="path">The path of the configuration file to read from</param>
        /// <returns>The new Configuration object</returns>
        public static Configuration Read(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    // Create a new XML Serializer
                    var serializer = new XmlSerializer(typeof(Configuration));

                    // Create a new FileStream to Open the configuration file for reading
                    using (var fileReader = new FileStream(path, FileMode.Open))
                    using (var xmlReader = XmlReader.Create(fileReader))
                    {
                        // Deserialize the Configuration object using the XML Serializer
                        var config = (Configuration)serializer.Deserialize(xmlReader);

                        // Set the path that the configuration was read from
                        config._path = path;

                        // Return the new Configuration object
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }           

            return null;
        }

        /// <summary>
        /// Save the Configuration back to the original file path it was initially read from. Overwrites original file.
        /// </summary>
        public void Save(string path = null)
        {
            var savePath = path;
            if (string.IsNullOrEmpty(path)) savePath = Path;

            if (!string.IsNullOrEmpty(savePath))
            {
                try
                {
                    // Create a new XML Serializer
                    var serializer = new XmlSerializer(typeof(Configuration));

                    // Create a new FileStream to Create/Overwrite the file
                    using (var fileWriter = new FileStream(savePath, FileMode.Create))
                    using (var xmlWriter = XmlWriter.Create(fileWriter, new XmlWriterSettings() { Indent = true }))
                    {
                        // Serialize the Configuration object to XML
                        serializer.Serialize(xmlWriter, this);
                    }

                    log.Info("Configuration Saved : " + savePath);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
            else log.Warn("Configuration could not be saved. No Path is set.");
        }
    }
}
