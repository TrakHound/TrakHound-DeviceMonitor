// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace TrakHound.TempServer.MTConnect
{
    class MTConnectConnectionStartQueue
    {
        private ConcurrentDictionary<string, MTConnectConnection> queue = new ConcurrentDictionary<string, MTConnectConnection>();
        private ManualResetEvent stop;
        private Thread thread;

        public delegate void ConnectionStartedHandler(MTConnectConnection connection);
        public event ConnectionStartedHandler ConnectionStarted;

        public int Delay { get; set; }

        public int Count
        {
            get
            {
                if (queue != null) return queue.Count;
                return -1;
            }
        }


        public MTConnectConnectionStartQueue()
        {
            Delay = 1000;
        }

        public void Start()
        {
            stop = new ManualResetEvent(false);

            thread = new Thread(new ThreadStart(Worker));
            thread.Start();
        }

        public void Stop()
        {
            if (stop != null) stop.Set();
        }

        public void Add(MTConnectConnection connection)
        {
            if (connection != null)
            {
                queue.GetOrAdd(connection.DeviceId, connection);
            }
        }

        private void Worker()
        {
            do
            {
                var connections = queue.Select(o => o.Value).ToList();

                if (connections != null && connections.Count > 0)
                {
                    var connection = connections[0];

                    // Start the MTConnectConnection
                    connection.Start();

                    // Raise event to notify that the connection has started
                    ConnectionStarted?.Invoke(connection);

                    // Remove from queue
                    MTConnectConnection dummy = null;
                    queue.TryRemove(connection.DeviceId, out dummy);
                }
            } while (!stop.WaitOne(Delay, true));
        }
    }
}
