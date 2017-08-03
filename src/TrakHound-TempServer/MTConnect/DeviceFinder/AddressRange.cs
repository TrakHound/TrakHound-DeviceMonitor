// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using Newtonsoft.Json;
using System.Xml.Serialization;

namespace TrakHound.TempServer.MTConnect.DeviceFinder
{
    /// <summary>
    /// Range of IP Addresses
    /// </summary>
    public class AddressRange
    {
        [XmlAttribute("minimum")]
        [JsonProperty("minimum")]
        public string Minimum { get; set; }

        [XmlAttribute("maximum")]
        [JsonProperty("maximum")]
        public string Maximum { get; set; }

        [XmlArray("Allow")]
        [XmlArrayItem("Address")]
        [JsonProperty("allow")]
        public string[] AllowedAddresses { get; set; }

        [XmlArray("Deny")]
        [XmlArrayItem("Address")]
        [JsonProperty("deny")]
        public string[] DeniedAddresses { get; set; }

        public AddressRange() { }

        public AddressRange(string minimum, string maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }
    }
}
