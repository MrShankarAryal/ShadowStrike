using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class AmplificationScanner
    {
        public async Task<Dictionary<string, string>> ScanAsync(string ip)
        {
            var results = new Dictionary<string, string>();
            
            // DNS (53)
            var dnsFactor = await ProbeAsync(ip, 53, new byte[] { 
                0x00, 0x00, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x02, 0x00, 0x01 
            }); // Standard Query ANY .
            if (dnsFactor > 1.0) results.Add("DNS", $"Vulnerable ({dnsFactor:F1}x)");

            // NTP (123)
            var ntpFactor = await ProbeAsync(ip, 123, new byte[] { 0x17, 0x00, 0x03, 0x2a } // Monlist
            );
            if (ntpFactor > 1.0) results.Add("NTP", $"Vulnerable ({ntpFactor:F1}x)");

            // Memcached (11211)
            var memFactor = await ProbeAsync(ip, 11211, new byte[] { 
                0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x73, 0x74, 0x61, 0x74, 0x73, 0x0d, 0x0a 
            }); // stats
            if (memFactor > 1.0) results.Add("Memcached", $"Vulnerable ({memFactor:F1}x)");

            return results;
        }

        private async Task<double> ProbeAsync(string ip, int port, byte[] payload)
        {
            try
            {
                using (var client = new UdpClient())
                {
                    client.Client.ReceiveTimeout = 2000;
                    client.Connect(ip, port);
                    await client.SendAsync(payload, payload.Length);

                    var receiveTask = client.ReceiveAsync();
                    if (await Task.WhenAny(receiveTask, Task.Delay(2000)) == receiveTask)
                    {
                        var result = await receiveTask;
                        if (result.Buffer.Length > 0)
                        {
                            return (double)result.Buffer.Length / payload.Length;
                        }
                    }
                }
            }
            catch { }
            return 0.0;
        }
    }
}
