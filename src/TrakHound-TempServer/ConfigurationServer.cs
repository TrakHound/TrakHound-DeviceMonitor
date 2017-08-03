// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Web;

namespace TrakHound.TempServer
{
    public class ConfigurationServer
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private HttpListener listener;
        private ManualResetEvent stop;

        public delegate void ConfigurationUpdatedHandler(Configuration config);
        public event ConfigurationUpdatedHandler ConfigurationUpdated;

        public void Start()
        {
            log.Info("Configuration Server Started..");

            stop = new ManualResetEvent(false);

            var thread = new Thread(new ThreadStart(Worker));
            thread.Start();
        }

        public void Stop()
        {
            if (stop != null) stop.Set();
            if (listener != null) listener.Close();
        }

        
        private void Worker()
        {
            do
            {
                if (listener == null || !listener.IsListening) ListenForRequests();

            } while (!stop.WaitOne(5000, true));
        }

        private void ListenForRequests()
        {
            listener = new HttpListener();

            try
            {
                // (Access Denied - Exception)
                // Must grant permissions to use URL (for each Prefix) in Windows using the command below
                // CMD: netsh http add urlacl url = "http://localhost/" user = everyone

                // (Service Unavailable - HTTP Status)
                // Multiple urls are configured using netsh that point to the same place

                var prefix = "http://localhost:8479/";

                // Add Prefixes
                listener.Prefixes.Add(prefix);

                log.Info("Configuration Listener Starting : Listening at " + prefix + "..");

                // Start Listener
                listener.Start();

                var listenTask = new Task(() =>
                {
                    while (listener.IsListening && !stop.WaitOne(0, true))
                    {
                        try
                        {
                            var context = listener.GetContext();
                                Task.Factory.StartNew(() => HandleRequest(context));
                            }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                        }
                    }
                });
                listenTask.Start();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
        
        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                log.Info("Connected to : " + context.Request.LocalEndPoint.ToString() + " : " + context.Request.Url.ToString());

                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, DELETE");

                var uri = context.Request.Url;
                var method = context.Request.HttpMethod;

                switch (method)
                {
                    case "GET":

                        using (var stream = context.Response.OutputStream)
                        {
                            var segments = uri.Segments;
                            if (segments.Length > 0)
                            {
                                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.FILENAME);
                                var config = Configuration.Read(configPath);
                                if (config != null)
                                {
                                    // Check if Find Devices is requested
                                    if (segments[segments.Length - 1].ToLower().Trim('/') == "devices")
                                    {
                                        if (!config.DeviceFinders.IsNullOrEmpty())
                                        {
                                            var devices = new List<MTConnect.MTConnectConnection>();

                                            foreach (var deviceFinder in config.DeviceFinders)
                                            {
                                                deviceFinder.DeviceFound += (o, i) =>
                                                {
                                                    var deviceId = Server.GenerateDeviceId(i);
                                                    devices.Add(new MTConnect.MTConnectConnection(deviceId, i.IpAddress.ToString(), i.Port, i.MacAddress.ToString(), i.DeviceName));
                                                };
                                                deviceFinder.Start(false);
                                            }

                                            if (!devices.IsNullOrEmpty())
                                            {
                                                var json = Json.Convert.ToJson(devices);
                                                if (!string.IsNullOrEmpty(json))
                                                {
                                                    var bytes = Encoding.UTF8.GetBytes(json);
                                                    stream.Write(bytes, 0, bytes.Length);

                                                    context.Response.StatusCode = 200;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var json = Json.Convert.ToJson(config);
                                        if (!string.IsNullOrEmpty(json))
                                        {
                                            var bytes = Encoding.UTF8.GetBytes(json);
                                            stream.Write(bytes, 0, bytes.Length);

                                            context.Response.StatusCode = 200;
                                        }
                                        else context.Response.StatusCode = 500;
                                    }
                                }
                                else context.Response.StatusCode = 404;
                            }

                            log.Info("Rest Response : " + context.Response.StatusCode);
                        }

                        break;

                    case "POST":

                        using (var stream = context.Request.InputStream)
                        using (var streamReader = new StreamReader(stream))
                        {
                            var json = streamReader.ReadToEnd();
                            if (!string.IsNullOrEmpty(json))
                            {
                                json = HttpUtility.UrlDecode(json);

                                var config = Json.Convert.FromJson<Configuration>(json);
                                if (config != null)
                                {
                                    context.Response.StatusCode = 200;
                                    ConfigurationUpdated?.Invoke(config);
                                }
                                else context.Response.StatusCode = 400;
                            }
                            else context.Response.StatusCode = 400;

                            log.Info("Rest Response : " + context.Response.StatusCode);
                        }

                        break;
                }

                context.Response.Close();
            }
            catch (Exception ex)
            {
                log.Debug(ex);
            }
        }
    }
}
