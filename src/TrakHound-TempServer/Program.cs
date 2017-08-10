// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;

namespace TrakHound.TempServer
{
    static class Program
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static Server server;
        private static ServiceBase service;
        private static RestServer restServer;
        private static ConfigurationServer configurationServer;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            Init(args);
        }

        private static void Init(string[] args)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
            System.Threading.ThreadPool.SetMinThreads(100, 4);

            if (args.Length > 0)
            {
                string mode = args[0];

                switch (mode)
                {
                    // Debug (Run as console application)
                    case "debug":

                        Start();
                        Console.ReadLine();
                        Stop();
                        Console.ReadLine();
                        break;

                    // Install the Service
                    case "install":

                        InstallService();
                        break;

                    // Uninstall the Service
                    case "uninstall":

                        UninstallService();
                        break;
                }
            }
            else
            {
                StartService();
            }
        }

        public static void StartService()
        {
            if (service == null) service = new TempServerService();
            ServiceBase.Run(service);
        }

        public static void StopService()
        {
            if (service != null) service.Stop();
        }

        public static void Start()
        {
            PrintHeader();

            // Get the default Configuration file path
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.FILENAME);
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.DEFAULT_FILENAME);
            if (!File.Exists(configPath) && File.Exists(defaultPath))
            {
                var defaultConfig = Configuration.Read(defaultPath);
                defaultConfig.Save(configPath);
            }
            var config = Configuration.Read(configPath);
            if (config != null)
            {
                // Initialize the Database Configuration
                var databaseModule = new DatabaseModule();
                databaseModule.Initialize(null);
                Api.v2.Database.Module = databaseModule;

                log.Info("Configuration file read from '" + configPath + "'");
                log.Info("---------------------------");

                // Start Configuration Server
                configurationServer = new ConfigurationServer();
                configurationServer.ConfigurationUpdated += ConfigurationServer_ConfigurationUpdated;
                configurationServer.Start();

                // Create a new RestServer
                restServer = new RestServer(config);
                restServer.Start();

                // Create a new Server
                server = new Server(config);
                server.Start();
            }
            else
            {
                // Throw exception that no configuration file was found
                var ex = new Exception("No Configuration File Found. Exiting TrakHound Predix Server!");
                log.Error(ex);
                throw ex;
            }
        }

        private static void ConfigurationServer_ConfigurationUpdated(Configuration config)
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.FILENAME);
            config.Save(configPath);

            if (server != null)
            {
                if (config.Devices.IsNullOrEmpty())
                {
                    server.StopMTConnectDevices();
                }
                else
                {
                    foreach (var device in config.Devices)
                    {
                        if (!server.Connections.Exists(o => o.DeviceId == device.DeviceId))
                        {
                            server.StartMTConnectConnection(device);
                        }
                    }

                    foreach (var connection in server.Connections)
                    {
                        if (!config.Devices.Exists(o => o.DeviceId == connection.DeviceId))
                        {
                            server.StopMTConnectConnection(connection.DeviceId);
                        }
                    }
                }
            }
        }

        public static void Stop()
        {
            if (server != null) server.Stop();
            if (server != null) restServer.Stop();
            if (configurationServer != null) configurationServer.Stop();
        }


        private static void InstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
        }

        private static void PrintHeader()
        {
            log.Info("---------------------------");
            log.Info("TrakHound Temp Server : v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            log.Info(@"Copyright 2017 TrakHound Inc., All Rights Reserved");
            log.Info("---------------------------");
        }
    }
}
