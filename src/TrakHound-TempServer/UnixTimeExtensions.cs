// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace TrakHound.TempServer
{
    public static class UnixTimeExtensions
    {
        public static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTime(this DateTime d)
        {
            return System.Convert.ToInt64(Math.Round((d - EpochTime).TotalMilliseconds, 0));
        }

        public static DateTime FromUnixTime(long unixMilliseconds)
        {
            return EpochTime.AddMilliseconds(unixMilliseconds);
        }
    }
}
