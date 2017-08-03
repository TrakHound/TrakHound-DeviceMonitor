// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using Newtonsoft.Json;
using System.Xml.Serialization;

namespace TrakHound.TempServer.MTConnect.DeviceFinder
{
    /// <summary>
    /// Range of TCP Ports
    /// </summary>
    public class PortRange
    {
        [XmlAttribute("minimum")]
        [JsonProperty("minimum")]
        public int Minimum { get; set; }

        [XmlAttribute("maximum")]
        [JsonProperty("maximum")]
        public int Maximum { get; set; }
        
        [XmlArray("Allow")]
        [XmlArrayItem("Port")]
        [JsonProperty("allow")]
        public int[] AllowedPorts { get; set; }

        [XmlArray("Deny")]
        [XmlArrayItem("Port")]
        [JsonProperty("deny")]
        public int[] DeniedPorts { get; set; }

        public PortRange() { }

        public PortRange(int minimum, int maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }
    }
}
