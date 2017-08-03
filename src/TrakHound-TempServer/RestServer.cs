// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TrakHound.TempServer
{
    public class RestServer
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private HttpListener listener;
        private ManualResetEvent stop;
        private Configuration configuration;

        public List<string> Prefixes { get; set; }

        public RestServer(Configuration config)
        {
            configuration = config;
            Prefixes = config.Prefixes;

            // Load the REST Modules
            Modules.Load();
        }

        public void Start()
        {
            log.Info("REST Server Started..");

            if (Prefixes != null && Prefixes.Count > 0)
            {
                stop = new ManualResetEvent(false);

                var thread = new Thread(new ThreadStart(Worker));
                thread.Start();
            }
            else
            {
                var ex = new Exception("No URL Prefixes are defined!");
                log.Error(ex);
                throw ex;
            }
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

                // Add Prefixes
                foreach (var prefix in Prefixes)
                {
                    listener.Prefixes.Add(prefix);
                }

                log.Info("REST Listener Starting");

                // Start Listener
                listener.Start();

                foreach (var prefix in Prefixes) log.Info("Rest Server : Listening at " + prefix + "..");

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
                            context.Response.StatusCode = 200;
                            bool found = false;

                            foreach (var module in Modules.LoadedModules)
                            {
                                try
                                {
                                    var m = Modules.Get(module.GetType());
                                    if (m.GetResponse(uri, stream))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    log.Info(module.Name + " : ERROR : " + ex.Message);
                                }
                            }

                            if (!found) context.Response.StatusCode = 400;

                            log.Info("Rest Response : " + context.Response.StatusCode);
                        }

                        break;

                    case "POST":

                        using (var stream = context.Request.InputStream)
                        {
                            context.Response.StatusCode = 200;
                            bool found = false;

                            foreach (var module in Modules.LoadedModules)
                            {
                                var m = Modules.Get(module.GetType());
                                if (m.SendData(uri, stream))
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found) context.Response.StatusCode = 400;

                            log.Info("Rest Response : " + context.Response.StatusCode);
                        }

                        break;

                    case "DELETE":

                        context.Response.StatusCode = 400;

                        foreach (var module in Modules.LoadedModules)
                        {
                            var m = Modules.Get(module.GetType());
                            if (m.DeleteData(uri))
                            {
                                context.Response.StatusCode = 200;
                                break;
                            }
                        }

                        log.Info("Rest Response : " + context.Response.StatusCode);

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
