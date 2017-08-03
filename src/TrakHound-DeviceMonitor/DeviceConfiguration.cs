// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

namespace TrakHound.DeviceMonitor
{
    public class DeviceConfiguration
    {
        public string DeviceId { get; set; }

        public bool Enabled { get; set; }

        public int Index { get; set; }

        public bool PerformanceEnabled { get; set; }

        public bool QualityEnabled { get; set; }


        public DeviceConfiguration()
        {
            Init();
        }

        public DeviceConfiguration(string deviceId)
        {
            Init();
            DeviceId = deviceId;
        }

        private void Init()
        {
            Enabled = false;
            Index = -1;
            PerformanceEnabled = true;
            QualityEnabled = false;
        }
    }
}
