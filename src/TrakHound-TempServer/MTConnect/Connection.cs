// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Newtonsoft.Json;

namespace TrakHound.TempServer.MTConnect
{
    public class Connection
    {
        public string DeviceId { get; set; }

        public string Address { get; set; }

        public string PhysicalAddress { get; set; }

        public int Port { get; set; }
    }
}
