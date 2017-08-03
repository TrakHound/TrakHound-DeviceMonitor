// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Text.RegularExpressions;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;
using TrakHound.Api.v2.Streams;
using TrakHound.Api.v2.Streams.Data;
using Json = TrakHound.Api.v2.Json;

namespace TrakHound.TempServer
{
    [InheritedExport(typeof(IDatabaseModule))]
    public class DatabaseModule : IDatabaseModule
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        internal static string connectionString;

        private Configuration configuration;

        /// <summary>
        /// Gets the name of the Database. This corresponds to the node name in the 'server.config' file
        /// </summary>
        public string Name { get { return "Sqlite"; } }


        public bool Initialize(string databaseConfigurationPath)
        {
            configuration = Configuration.Current;

            return true;
        }

        public void Close() { }

        #region "Read"

        private static T Read<T>(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                try
                {
                    // Create a new SqlConnection using the connectionString
                    using (var connection = new SQLiteConnection(connectionString))
                    {
                        // Open the connection
                        connection.Open();

                        using (var command = new SQLiteCommand(query, connection))
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            reader.Read();
                            return Read<T>(reader);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("SQLite Query Error : " + query);
                    logger.Error(ex);
                }
            }

            return default(T);
        }

        private static List<T> ReadList<T>(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                try
                {
                    var list = new List<T>();

                    // Create a new SqlConnection using the connectionString
                    using (var connection = new SQLiteConnection(connectionString))
                    {
                        // Open the connection
                        connection.Open();

                        using (var command = new SQLiteCommand(query, connection))
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(Read<T>(reader));
                            }
                        }
                    }

                    return list;
                }
                catch (Exception ex)
                {
                    logger.Error("SQLite Query Error : " + query);
                    logger.Error(ex);
                }
            }

            return null;
        }

        private static T Read<T>(SQLiteDataReader reader)
        {
            var obj = (T)Activator.CreateInstance(typeof(T));

            // Get object's properties
            var properties = typeof(T).GetProperties().ToList();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var column = reader.GetName(i);
                var value = reader.GetValue(i);

                var property = properties.Find(o => PropertyToColumn(o.Name) == column);
                if (property != null && value != null)
                {
                    object val = default(T);

                    if (property.PropertyType == typeof(string))
                    {
                        string s = value.ToString();
                        if (!string.IsNullOrEmpty(s)) val = s;
                    }
                    else if (property.PropertyType == typeof(DateTime))
                    {
                        long ms = (long)value;
                        val = UnixTimeExtensions.EpochTime.AddMilliseconds(ms);
                    }
                    else
                    {
                        val = Convert.ChangeType(value, property.PropertyType);
                    }

                    property.SetValue(obj, val, null);
                }
            }

            return obj;
        }

        private static string PropertyToColumn(string propertyName)
        {
            if (propertyName != propertyName.ToUpper())
            {
                // Split string by Uppercase characters
                var parts = Regex.Split(propertyName, @"(?<!^)(?=[A-Z])");
                string s = string.Join("_", parts);
                return s.ToLower();
            }
            else return propertyName.ToLower();
        }

        /// <summary>
        /// Read the most current AgentDefintion from the database
        /// </summary>
        public AgentDefinition ReadAgent(string deviceId)
        {
            return Server.ReadAgent(deviceId);
        }

        /// <summary>
        /// Read AssetDefintions from the database
        /// </summary>
        public List<AssetDefinition> ReadAssets(string deviceId, string assetId, DateTime from, DateTime to, DateTime at, long count)
        {
            var assets = new List<AssetDefinition>();

            return assets;
        }

        /// <summary>
        /// Read the ComponentDefinitions for the specified Agent Instance Id from the database
        /// </summary>
        public List<ComponentDefinition> ReadComponents(string deviceId, long agentInstanceId)
        {
            return Server.ReadComponents(deviceId);
        }

        /// <summary>
        /// Read all of the Connections available from the DataServer
        /// </summary>
        public List<ConnectionDefinition> ReadConnections()
        {
            return Server.ReadConnections();
        }

        /// <summary>
        /// Read the most ConnectionDefintion from the database
        /// </summary>
        public ConnectionDefinition ReadConnection(string deviceId)
        {
            return Server.ReadConnection(deviceId);
        }

        /// <summary>
        /// Read the DataItemDefinitions for the specified Agent Instance Id from the database
        /// </summary>
        public List<DataItemDefinition> ReadDataItems(string deviceId, long agentInstanceId)
        {
            return Server.ReadDataItems(deviceId);
        }

        /// <summary>
        /// Read the DeviceDefintion for the specified Agent Instance Id from the database
        /// </summary>
        public DeviceDefinition ReadDevice(string deviceId, long agentInstanceId)
        {
            return Server.ReadDevice(deviceId);
        }

        /// <summary>
        /// Read Samples from the database
        /// </summary>
        public List<Sample> ReadSamples(string[] dataItemIds, string deviceId, DateTime from, DateTime to, DateTime at, long count)
        {
            return Server.ReadSamples(deviceId, from, to);
        }

        /// <summary>
        /// Read RejectedParts from the database
        /// </summary>
        public List<RejectedPart> ReadRejectedParts(string deviceId, string[] partIds, DateTime from, DateTime to, DateTime at)
        {
            var parts = new List<RejectedPart>();

            return parts;
        }

        /// <summary>
        /// Read VerifiedParts from the database
        /// </summary>
        public List<VerifiedPart> ReadVerifiedParts(string deviceId, string[] partIds, DateTime from, DateTime to, DateTime at)
        {
            var parts = new List<VerifiedPart>();

            return parts;
        }

        /// <summary>
        /// Read the Status from the database
        /// </summary>
        public Status ReadStatus(string deviceId)
        {
            return Server.ReadStatus(deviceId);
        }

        #endregion

        #region "Write"

        /// <summary>
        /// Write ConnectionDefintions to the database
        /// </summary>
        public bool Write(List<ConnectionDefinitionData> definitions)
        {
            return true;
        }

        /// <summary>
        /// Write AgentDefintions to the database
        /// </summary>
        public bool Write(List<AgentDefinitionData> definitions)
        {
            return true;
        }

        /// <summary>
        /// Write AssetDefintions to the database
        /// </summary>
        public bool Write(List<AssetDefinitionData> definitions)
        {
            return true;
        }

        /// <summary>
        /// Write ComponentDefintions to the database
        /// </summary>
        public bool Write(List<ComponentDefinitionData> definitions)
        {
            return true;
        }

        /// <summary>
        /// Write DeviceDefintions to the database
        /// </summary>
        public bool Write(List<DeviceDefinitionData> definitions)
        {
            return true;
        }

        /// <summary>
        /// Write DataItemDefinitions to the database
        /// </summary>
        public bool Write(List<DataItemDefinitionData> definitions)
        {
            return true;
        }

        /// <summary>
        /// Write Samples to the database
        /// </summary>
        public bool Write(List<SampleData> samples)
        {
            return true;
        }

        /// <summary>
        /// Write RejectedParts to the database
        /// </summary>
        public bool Write(List<RejectedPart> parts)
        {
            return true;
        }

        /// <summary>
        /// Write VerifiedParts to the database
        /// </summary>
        public bool Write(List<VerifiedPart> parts)
        {
            return true;
        }

        /// <summary>
        /// Write StatusData to the database
        /// </summary>
        public bool Write(List<StatusData> statuses)
        {
            return true;
        }

        #endregion

        #region "Delete"

        /// <summary>
        /// Delete RejectedParts from the database
        /// </summary>
        public bool DeleteRejectedPart(string deviceId, string partId)
        {
            return false;
        }

        /// <summary>
        /// Delete VerifiedParts from the database
        /// </summary>
        public bool DeleteVerifiedPart(string deviceId, string partId)
        {
            return false;
        }

        #endregion
    }
}
