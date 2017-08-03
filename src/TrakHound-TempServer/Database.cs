// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TrakHound.TempServer.MTConnect;
using TrakHound.Api.v2.Data;

namespace TrakHound.TempServer
{
    class Database
    {
        private const string CONNECTION_FORMAT = "Data Source={0}; Pooling=True; Max Pool Size=100; PRAGMA journal_mode=WAL;";

        private static Logger log = LogManager.GetCurrentClassLogger();
        private static string databasePath;
        private static string connectionString;

        public static void Initialize()
        {
            // Get Assembly Path
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            databasePath = Path.Combine(assemblyPath, "tempserver-backup.db");
            connectionString = string.Format(CONNECTION_FORMAT, databasePath);

            if (!File.Exists(databasePath))
            {
                // Create the Database File
                SQLiteConnection.CreateFile(databasePath);
                log.Info("SQLite Database File Created at : " + databasePath);

                // Run Table creation queries
                var queryFilePath = Path.Combine(assemblyPath, "sqlite-create-tables.sql");

                if (File.Exists(queryFilePath))
                {
                    log.Info("Create Tables query found at : " + queryFilePath);
                    var query = File.ReadAllText(queryFilePath);
                    ExecuteQuery(query);
                }
                else
                {
                    log.Info("No Create Tables query found at : " + queryFilePath);
                }
            }
        }

        public static void ExecuteQuery(string query)
        {
            try
            {
                // Create a new SqlConnection using the connectionString
                using (var connection = new SQLiteConnection(connectionString))
                using (var command = new SQLiteCommand(query))
                {
                    // Open the connection
                    connection.Open();
                    command.CommandTimeout = 300;
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (SQLiteException ex) { log.Warn(ex); }
            catch (Exception ex) { log.Error(ex); }
        }

        public static void ExecuteTransaction(List<string> queries)
        {
            try
            {
                // Create a new SqlConnection using the connectionString
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (var tr = connection.BeginTransaction())
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandTimeout = 300;
                            command.Transaction = tr;

                            foreach (var query in queries)
                            {
                                command.CommandText = query;
                                command.ExecuteNonQuery();
                            }
                        }

                        tr.Commit();
                    }

                    connection.Close();
                }
            }
            catch (SQLiteException ex) { log.Warn(ex); }
            catch (Exception ex) { log.Error(ex); }
        }


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
                    log.Error("SQLite Query Error : " + query);
                    log.Error(ex);
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
                    log.Error("SQLite Query Error : " + query);
                    log.Error(ex);
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

        public static List<AgentDefinition> ReadAgents()
        {
            string query = "SELECT * FROM `agents`";
            return ReadList<AgentDefinition>(query);
        }

        public static List<ComponentDefinition> ReadComponents()
        {
            string query = "SELECT * FROM `components`";
            return ReadList<ComponentDefinition>(query);
        }

        public static List<ConnectionDefinition> ReadConnections()
        {
            string query = "SELECT * FROM `connections`";
            return ReadList<ConnectionDefinition>(query);
        }

        public static List<DataItemDefinition> ReadDataItems()
        {
            string query = "SELECT * FROM `data_items`";
            return ReadList<DataItemDefinition>(query);
        }

        public static List<DeviceDefinition> ReadDevices()
        {
            string query = "SELECT * FROM `devices`";
            return ReadList<DeviceDefinition>(query);
        }

        public static List<Sample> ReadSamples()
        {
            string query = "SELECT * FROM `samples`";
            return ReadList<Sample>(query);
        }

        #endregion

        #region "Write"

        public static bool Write(List<SQLiteCommand> commands)
        {
            try
            {
                // Create a new SqlConnection using the connectionString
                using (var connection = new SQLiteConnection(connectionString))
                {
                    // Open the connection
                    connection.Open();

                    using (var tr = connection.BeginTransaction())
                    {
                        foreach (var command in commands)
                        {
                            command.CommandTimeout = 300;
                            command.Connection = connection;
                            command.Transaction = tr;
                            command.ExecuteNonQuery();
                            command.Dispose();
                        }

                        tr.Commit();
                    }

                    connection.Close();

                    return true;
                }
            }
            catch (SQLiteException ex) { log.Warn(ex); }
            catch (Exception ex) { log.Error(ex); }

            return false;
        }


        /// <summary>
        /// Write Connections to the database
        /// </summary>
        public static bool Write(List<ConnectionDefinition> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`, `address`, `port`, `physical_address`";
                string VALUES = "(@deviceId, @address, @port, @physicalAddress)";

                string QUERY_FORMAT = "INSERT OR REPLACE INTO `connections` ({0}) VALUES {1}";
                string query = string.Format(QUERY_FORMAT, COLUMNS, VALUES);

                var commands = new List<SQLiteCommand>();

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    var command = new SQLiteCommand(query);
                    command.Parameters.AddWithValue("@deviceId", d.DeviceId ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@address", d.Address ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@port", d.Port);
                    command.Parameters.AddWithValue("@physicalAddress", d.PhysicalAddress ?? Convert.DBNull);
                    commands.Add(command);
                }

                return Write(commands);
            }

            return false;
        }

        /// <summary>
        /// Write Agents to the database
        /// </summary>
        public static bool Write(List<AgentDefinition> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`, `instance_id`, `sender`, `version`, `buffer_size`, `test_indicator`, `timestamp`";
                string VALUES = "(@deviceId, @instanceId, @sender, @version, @bufferSize, @testIndicator, @timestamp)";

                string QUERY_FORMAT = "INSERT OR REPLACE INTO `agents` ({0}) VALUES {1}";
                var query = string.Format(QUERY_FORMAT, COLUMNS, VALUES);

                var commands = new List<SQLiteCommand>();

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    var command = new SQLiteCommand(query);
                    command.Parameters.AddWithValue("@deviceId", d.DeviceId ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@instanceId", d.InstanceId);
                    command.Parameters.AddWithValue("@sender", d.Sender ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@version", d.Version ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@bufferSize", d.BufferSize);
                    command.Parameters.AddWithValue("@testIndicator", d.TestIndicator ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@timestamp", d.Timestamp.ToUnixTime());
                    commands.Add(command);
                }

                return Write(commands);
            }

            return false;
        }

        /// <summary>
        /// Write Components to the database
        /// </summary>
        public static bool Write(List<ComponentDefinition> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`, `agent_instance_id`, `id`, `uuid`, `name`, `native_name`, `sample_interval`, `sample_rate`, `type`, `parent_id`";
                string VALUES = "(@deviceId, @agentInstanceId, @id, @uuid, @name, @nativeName, @sampleInterval, @sampleRate, @type, @parentId)";

                string QUERY_FORMAT = "INSERT OR REPLACE INTO `components` ({0}) VALUES {1}";
                string query = string.Format(QUERY_FORMAT, COLUMNS, VALUES);

                var commands = new List<SQLiteCommand>();

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    var command = new SQLiteCommand(query);
                    command.Parameters.AddWithValue("@deviceId", d.DeviceId ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@agentInstanceId", d.AgentInstanceId);
                    command.Parameters.AddWithValue("@id", d.Id ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@uuid", d.Uuid ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@name", d.Name ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@nativeName", d.NativeName ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@sampleInterval", d.SampleInterval);
                    command.Parameters.AddWithValue("@sampleRate", d.SampleRate);
                    command.Parameters.AddWithValue("@type", d.Type ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@parentId", d.ParentId ?? Convert.DBNull);
                    commands.Add(command);
                }

                return Write(commands);
            }

            return false;
        }

        /// <summary>
        /// Write Devices to the database
        /// </summary>
        public static bool Write(List<DeviceDefinition> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`, `agent_instance_id`, `id`, `uuid`, `name`, `native_name`, `sample_interval`, `sample_rate`, `iso_841_class`, `manufacturer`, `model`, `serial_number`, `station`, `description`";
                string VALUES = "(@deviceId, @agentInstanceId, @id, @uuid, @name, @nativeName, @sampleInterval, @sampleRate, @iso841Class, @manufacturer, @model, @serialNumber, @station, @description)";

                string QUERY_FORMAT = "INSERT OR REPLACE INTO `devices` ({0}) VALUES {1}";
                string query = string.Format(QUERY_FORMAT, COLUMNS, VALUES);

                var commands = new List<SQLiteCommand>();

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    var command = new SQLiteCommand(query);
                    command.Parameters.AddWithValue("@deviceId", d.DeviceId ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@agentInstanceId", d.AgentInstanceId);
                    command.Parameters.AddWithValue("@id", d.Id ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@uuid", d.Uuid ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@name", d.Name ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@nativeName", d.NativeName ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@sampleInterval", d.SampleInterval);
                    command.Parameters.AddWithValue("@sampleRate", d.SampleRate);
                    command.Parameters.AddWithValue("@iso841Class", d.Iso841Class ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@manufacturer", d.Manufacturer ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@model", d.Model ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@serialNumber", d.SerialNumber ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@station", d.Station ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@description", d.Description ?? Convert.DBNull);
                    commands.Add(command);
                }

                return Write(commands);
            }

            return false;
        }

        /// <summary>
        /// Write DataItems to the database
        /// </summary>
        public static bool Write(List<DataItemDefinition> definitions)
        {
            if (!definitions.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`, `agent_instance_id`, `id`, `name`, `category`, `type`, `sub_type`, `statistic`, `units`, `native_units`, `native_scale`, `coordinate_system`, `sample_rate`, `representation`, `significant_digits`, `parent_id`";
                string VALUES = "(@deviceId, @agentInstanceId, @id, @name, @category, @type, @subType, @statistic, @units, @nativeUnits, @nativeScale, @coordinateSystem, @sampleRate, @representation, @significantDigits, @parentId)";

                string QUERY_FORMAT = "INSERT OR REPLACE INTO `data_items` ({0}) VALUES {1}";
                string query = string.Format(QUERY_FORMAT, COLUMNS, VALUES);

                var commands = new List<SQLiteCommand>();

                for (var i = 0; i < definitions.Count; i++)
                {
                    var d = definitions[i];

                    var command = new SQLiteCommand(query);
                    command.Parameters.AddWithValue("@deviceId", d.DeviceId ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@agentInstanceId", d.AgentInstanceId);
                    command.Parameters.AddWithValue("@id", d.Id ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@name", d.Name ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@category", d.Category ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@type", d.Type ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@subType", d.SubType ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@statistic", d.Statistic ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@units", d.Units ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@nativeUnits", d.NativeUnits ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@nativeScale", d.NativeScale ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@coordinateSystem", d.CoordinateSystem ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@sampleRate", d.SampleRate);
                    command.Parameters.AddWithValue("@representation", d.Representation ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@significantDigits", d.SignificantDigits);
                    command.Parameters.AddWithValue("@parentId", d.ParentId ?? Convert.DBNull);
                    commands.Add(command);
                }

                return Write(commands);
            }

            return false;
        }

        /// <summary>
        /// Write StreamDatas to the database
        /// </summary>
        public static bool Write(List<Sample> samples)
        {
            if (!samples.IsNullOrEmpty())
            {
                string COLUMNS = "`device_id`, `id`, `timestamp`, `agent_instance_id`, `sequence`, `cdata`, `condition`";
                string VALUES = "(@deviceId, @id, @timestamp, @agentInstanceId, @sequence, @cdata, @condition)";

                string QUERY_FORMAT = "INSERT OR REPLACE INTO `samples` ({0}) VALUES {1}";
                string query = string.Format(QUERY_FORMAT, COLUMNS, VALUES);

                var commands = new List<SQLiteCommand>();

                for (var i = 0; i < samples.Count; i++)
                {
                    var s = samples[i];

                    var command = new SQLiteCommand(query);
                    command.Parameters.AddWithValue("@deviceId", s.DeviceId ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@id", s.Id ?? Convert.DBNull);
                    command.Parameters.AddWithValue("@timestamp", s.Timestamp.ToUnixTime());
                    command.Parameters.AddWithValue("@agentInstanceId", s.AgentInstanceId);
                    command.Parameters.AddWithValue("@sequence", s.Sequence);
                    command.Parameters.AddWithValue("@cdata", s.CDATA.IsNullOrEmpty() ? Convert.DBNull : s.CDATA);
                    command.Parameters.AddWithValue("@condition", s.Condition ?? Convert.DBNull);
                    commands.Add(command);
                }

                return Write(commands);
            }
            else
            {
                return true;
            }
        }
        
        #endregion
    }
}
