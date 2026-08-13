namespace ShadowStrike.Core.Anonymity
{
    /// <summary>
    /// Defines the current anonymity operating mode of the application.
    /// </summary>
    public enum AnonymityMode
    {
        /// <summary>All traffic routes directly via the host network adapter.</summary>
        Off = 0,

        /// <summary>
        /// Lightweight mode: tor.exe is started directly on the host.
        /// Traffic is routed via SOCKS5 on 127.0.0.1:9050 using the custom
        /// TorSocks5Handler (SocketsHttpHandler.ConnectCallback-based, true socks5h semantics).
        /// This is the DEFAULT mode when Anonymous Mode is toggled on.
        /// NOTE: .NET 8 HttpClient does NOT natively support SOCKS5 — WebProxy("socks5://...")
        /// sends HTTP CONNECT to Tor's SOCKS port and silently fails. We use ConnectCallback.
        /// </summary>
        LightweightTor = 1,

        /// <summary>
        /// Hardened mode: Whonix-Gateway + Whonix-Workstation VMs are started via VirtualBox.
        /// Host is hardened (MAC, hostname, DNS, telemetry). All traffic is forced through
        /// the Tor circuit inside the Whonix-Gateway VM. LeakGuard runs every 60 seconds.
        /// </summary>
        HardenedWhonix = 2
    }

    /// <summary>Step-level progress report emitted during mode transitions.</summary>
    public record AnonymityProgress(int Step, int TotalSteps, string Message, bool IsError = false);
}
