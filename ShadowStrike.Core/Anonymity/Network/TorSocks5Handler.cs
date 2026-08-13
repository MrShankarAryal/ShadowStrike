using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowStrike.Core.Anonymity.Network
{
    /// <summary>
    /// Implements a true SOCKS5 proxy tunnel via <see cref="System.Net.Http.SocketsHttpHandler.ConnectCallback"/>.
    ///
    /// IMPORTANT — why this class exists:
    ///   .NET 8 <c>SocketsHttpHandler</c> does NOT natively support SOCKS5 proxies.
    ///   Using <c>new WebProxy("socks5://127.0.0.1:9050")</c> causes the handler to send
    ///   an HTTP CONNECT request to Tor's SOCKS port, which always fails.
    ///   This is a long-standing open runtime issue (dotnet/runtime#14098).
    ///
    ///   Instead, we provide the SOCKS5 handshake ourselves inside ConnectCallback,
    ///   and pass the *hostname* (not a resolved IP) in the SOCKS5 CONNECT request.
    ///   This gives true socks5h semantics: DNS resolution happens inside Tor, never
    ///   on the host — eliminating DNS leaks at the transport layer.
    /// </summary>
    public static class TorSocks5Handler
    {
        /// <summary>
        /// Creates a <see cref="System.Net.Http.SocketsHttpHandler"/> whose
        /// ConnectCallback performs the SOCKS5 handshake against <paramref name="socksHost"/>:<paramref name="socksPort"/>
        /// and passes the original hostname through so Tor resolves DNS (socks5h semantics).
        /// </summary>
        /// <param name="socksHost">SOCKS5 proxy host (default: 127.0.0.1)</param>
        /// <param name="socksPort">SOCKS5 proxy port (default: 9050)</param>
        public static System.Net.Http.SocketsHttpHandler Create(
            string socksHost = "127.0.0.1",
            int socksPort = 9050)
        {
            return new System.Net.Http.SocketsHttpHandler
            {
                // We are the proxy — disable the built-in proxy logic entirely so
                // SocketsHttpHandler does not try to interpret any WebProxy setting.
                UseProxy = false,
                AutomaticDecompression = System.Net.DecompressionMethods.All,
                ConnectCallback = async (ctx, ct) =>
                {
                    // 1. TCP connection to the SOCKS5 server (Tor)
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true
                    };
                    await socket.ConnectAsync(socksHost, socksPort, ct).ConfigureAwait(false);
                    var stream = new NetworkStream(socket, ownsSocket: true);

                    try
                    {
                        // ── Greeting ─────────────────────────────────────────────
                        // [VER=5, NMETHODS=1, METHOD=0x00 (no auth)]
                        await stream.WriteAsync(new byte[] { 0x05, 0x01, 0x00 }, ct).ConfigureAwait(false);

                        // ── Server method selection: [VER, METHOD] ────────────────
                        var method = new byte[2];
                        await ReadExactlyAsync(stream, method, ct).ConfigureAwait(false);
                        if (method[0] != 0x05 || method[1] != 0x00)
                            throw new InvalidOperationException(
                                $"SOCKS5 auth negotiation failed — Tor responded with method 0x{method[1]:X2}. " +
                                "Tor requires no-auth (0x00).");

                        // ── CONNECT request ────────────────────────────────────────
                        // We send the original *hostname* (ATYP=0x03), never a resolved IP.
                        // This means Tor resolves DNS inside the circuit — true socks5h semantics.
                        var host = ctx.DnsEndPoint.Host;
                        var port = (ushort)ctx.DnsEndPoint.Port;
                        var hostBytes = Encoding.UTF8.GetBytes(host);

                        if (hostBytes.Length > 255)
                            throw new InvalidOperationException(
                                $"Hostname '{host}' exceeds the 255-byte SOCKS5 limit.");

                        // [VER=5, CMD=1(CONNECT), RSV=0, ATYP=3(domainname), HOSTLEN, HOST..., PORT_HI, PORT_LO]
                        var req = new List<byte>
                        {
                            0x05, 0x01, 0x00, 0x03,
                            (byte)hostBytes.Length
                        };
                        req.AddRange(hostBytes);
                        req.Add((byte)(port >> 8));
                        req.Add((byte)(port & 0xFF));

                        await stream.WriteAsync(req.ToArray(), ct).ConfigureAwait(false);

                        // ── Reply header: [VER, REP, RSV, ATYP] ──────────────────
                        var header = new byte[4];
                        await ReadExactlyAsync(stream, header, ct).ConfigureAwait(false);

                        if (header[0] != 0x05)
                            throw new InvalidOperationException(
                                $"SOCKS5 reply has unexpected VER=0x{header[0]:X2}.");

                        if (header[1] != 0x00)
                        {
                            var reason = header[1] switch
                            {
                                0x01 => "general SOCKS server failure",
                                0x02 => "connection not allowed by ruleset",
                                0x03 => "network unreachable",
                                0x04 => "host unreachable",
                                0x05 => "connection refused",
                                0x06 => "TTL expired",
                                0x07 => "command not supported",
                                0x08 => "address type not supported",
                                _ => "unknown error"
                            };
                            throw new InvalidOperationException(
                                $"SOCKS5 CONNECT to {host}:{port} failed — REP=0x{header[1]:X2} ({reason}).");
                        }

                        // ── Drain the remainder of the SOCKS5 reply ────────────────
                        // ATYP determines how many extra bytes to consume before the 2-byte port.
                        int addrLen = header[3] switch
                        {
                            0x01 => 4,   // IPv4
                            0x04 => 16,  // IPv6
                            0x03 =>      // Domain name — one-byte length prefix
                                await ReadByteAsync(stream, ct).ConfigureAwait(false),
                            _ => throw new InvalidOperationException(
                                $"SOCKS5 reply contains unsupported ATYP=0x{header[3]:X2}.")
                        };

                        if (addrLen < 0)
                            throw new IOException("Truncated SOCKS5 reply (EOF reading domain length).");

                        // Drain BND.ADDR + BND.PORT (addrLen + 2 bytes)
                        await ReadExactlyAsync(stream, new byte[addrLen + 2], ct).ConfigureAwait(false);

                        // ── Tunnel is ready ────────────────────────────────────────
                        // HttpClient now speaks directly through the Tor circuit.
                        return stream;
                    }
                    catch
                    {
                        await stream.DisposeAsync().ConfigureAwait(false);
                        throw;
                    }
                }
            };
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>Reads exactly <paramref name="buf"/>.Length bytes from <paramref name="s"/>.</summary>
        private static async Task ReadExactlyAsync(Stream s, byte[] buf, CancellationToken ct)
        {
            int read = 0;
            while (read < buf.Length)
            {
                int n = await s.ReadAsync(buf.AsMemory(read, buf.Length - read), ct).ConfigureAwait(false);
                if (n <= 0)
                    throw new EndOfStreamException(
                        $"SOCKS5 stream ended prematurely (got {read} of {buf.Length} bytes).");
                read += n;
            }
        }

        /// <summary>Reads a single byte and returns it as int (or -1 on EOF).</summary>
        private static async ValueTask<int> ReadByteAsync(Stream s, CancellationToken ct)
        {
            var buf = new byte[1];
            int n = await s.ReadAsync(buf.AsMemory(0, 1), ct).ConfigureAwait(false);
            return n == 1 ? buf[0] : -1;
        }
    }
}
