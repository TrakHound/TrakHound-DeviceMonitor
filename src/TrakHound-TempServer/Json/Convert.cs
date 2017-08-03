// Copyright (c) 2017 TrakHound Inc, All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TrakHound.TempServer.Json
{
    public static class Convert
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public static T FromJson<T>(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var settings = new JsonSerializerSettings();
                    settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                    settings.DateParseHandling = DateParseHandling.DateTime;
                    settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    settings.NullValueHandling = NullValueHandling.Ignore;

                    return (T)JsonConvert.DeserializeObject(json, (typeof(T)), settings);
                }
                catch (JsonException ex) { log.Trace(ex); }
                catch (Exception ex) { log.Trace(ex); }
            }

            return default(T);
        }

        public static string ToJson(object data) { return ToJson(data, false, false, null); }

        public static string ToJson(object data, bool indent) { return ToJson(data, indent, false, null); }

        public static string ToJson(object data, bool indent, bool useIso8601) { return ToJson(data, indent, useIso8601, null); }

        public static string ToJson(object data, bool indent, bool useIso8601, List<JsonConverter> converters)
        {
            try
            {
                var settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;

                if (indent) settings.Formatting = Formatting.Indented;

                if (useIso8601)
                {
                    settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                    settings.DateParseHandling = DateParseHandling.DateTime;
                    settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                }

                if (converters != null)
                {
                    foreach (var converter in converters) settings.Converters.Add(converter);
                }

                return JsonConvert.SerializeObject(data, settings);
            }
            catch (JsonException ex) { log.Trace(ex); }
            catch (Exception ex) { log.Trace(ex); }

            return null;
        }

    }
}
