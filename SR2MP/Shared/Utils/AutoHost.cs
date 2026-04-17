// Modified of https://github.com/saltacc/'s PR to https://github.com/pyeight/SlimeRancher2Multiplayer/
// https://github.com/pyeight/SlimeRancher2Multiplayer/pull/32/

using System.Net;
using System.Net.NetworkInformation;
using SharpOpenNat;

namespace SR2MP.Shared.Utils;

internal sealed class AutoHostResult
{
    private AutoHostResult(bool success, ushort port, IPAddress? externalIp, string joinCode, string errorMessage)
    {
        Success = success;
        Port = port;
        ExternalIp = externalIp;
        JoinCode = joinCode;
        ErrorMessage = errorMessage;
    }

    internal bool Success { get; }
    internal ushort Port { get; }
    internal IPAddress? ExternalIp { get; }
    internal string JoinCode { get; }
    internal string ErrorMessage { get; }

    internal static AutoHostResult Failure(string message) => new AutoHostResult(false, 0, null, string.Empty, message);
    internal static AutoHostResult SuccessResult(ushort port, IPAddress externalIp, string joinCode) => new AutoHostResult(true, port, externalIp, joinCode, string.Empty);
}

internal static class AutoHost
{
    private const int RefreshIntervalSeconds = 3 * 60;
    private const int DiscoveryTimeoutSeconds = 10;
    private const int MappingTimeoutSeconds = 8;
    private const int MaxPortMapAttempts = 5;

    private static readonly HttpClient IpApiClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly object RefreshLock = new();
    private static readonly System.Random Rng = new();

    private static Timer? refreshTimer;
    private static INatDevice? refreshDevice;
    private static ushort refreshPort;

    internal static void BeginAutoHost(Action<AutoHostResult> onCompleted)
    {
        ArgumentNullException.ThrowIfNull(onCompleted, nameof(onCompleted));
        Task.Run(() =>
        {
            StopRefresh();
            var result = RunAutoHost();
            MainThreadDispatcher.Instance.Enqueue(() => onCompleted(result));
        });
    }

    private static void StopRefresh()
    {
        lock (RefreshLock)
        {
            if (refreshPort != 0)
                Firewall.RemoveException(refreshPort);

            refreshTimer?.Dispose();
            refreshTimer = null;
            refreshDevice = null;
            refreshPort = 0;
        }
    }

    private static AutoHostResult RunAutoHost()
    {
        SrLogger.LogMessage("UPnP: Starting auto host...");
        try
        {
            var externalIp = TryGetExternalIp();
            if (!IsUsableIp(externalIp))
            {
                SrLogger.LogWarning("UPnP: Could not determine external IP from API.");
                return AutoHostResult.Failure("Could not determine your external IP address.");
            }

            SrLogger.LogMessage("UPnP: External IP determined", $"UPnP: External IP determined: {externalIp}");

            var device = DiscoverDevice();
            if (device == null)
                return AutoHostResult.Failure("UPnP is not available on this network.");

            var port = TryMapPort(device);
            if (port == 0)
                return AutoHostResult.Failure("UPnP failed to map a port.");

            Firewall.AddException(port);
            var joinCode = JoinCode.Encode(externalIp!, port);
            SrLogger.LogMessage("UPnP: Join code generated!", $"UPnP: Join code generated for {externalIp}:{port}.");

            StartRefresh(device, port);
            return AutoHostResult.SuccessResult(port, externalIp!, joinCode);
        }
        catch (NatDeviceNotFoundException ex)
        {
            SrLogger.LogWarning($"UPnP: No device found: {ex.Message}");
            return AutoHostResult.Failure("UPnP is not available on this network.");
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"UPnP: Unexpected error: {ex.Message}");
            return AutoHostResult.Failure($"UPnP failed: {ex.Message}.");
        }
    }

    private static INatDevice? DiscoverDevice()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(DiscoveryTimeoutSeconds));
        var devices = OpenNat.Discoverer.DiscoverDevicesAsync(PortMapper.Upnp, cts.Token)
            .GetAwaiter().GetResult().ToList();

        if (devices.Count == 0)
        {
            SrLogger.LogWarning("UPnP: No devices discovered.");
            return null;
        }
        SrLogger.LogMessage($"UPnP: Discovered {devices.Count} device(s).");

        var gateways = GetGatewayAddresses();
        SrLogger.LogMessage("UPnP: System gateways discovered", $"UPnP: System gateways: {string.Join(", ", gateways)}");

        foreach (var device in devices)
        {
            var isGateway = gateways.Contains(device.HostEndPoint.Address);
            SrLogger.LogMessage($"UPnP: Device {device.HostEndPoint}{(isGateway ? " [GATEWAY MATCH]" : "")}", isGateway ? "Gateway match!" : "Gateway discovered");
            if (isGateway)
                return device;
        }

        SrLogger.LogWarning("UPnP: No device matched system gateway, falling back to first device.");
        return devices[0];
    }

    private static ushort TryMapPort(INatDevice device)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(MappingTimeoutSeconds));
        for (var attempt = 1; attempt <= MaxPortMapAttempts; attempt++)
        {
            var port = (ushort)Rng.Next(49152, 65536);
            try
            {
                var mapping = new Mapping(Protocol.Udp, port, port, 0, string.Empty);
                device.CreatePortMapAsync(mapping, cts.Token).GetAwaiter().GetResult();
                SrLogger.LogMessage($"UPnP: Mapped UDP port (attempt {attempt}).", $"UPnP: Mapped UDP port {port} (attempt {attempt})");
                return port;
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"UPnP: Port mapping attempt {attempt} failed: {ex.Message}");
            }
        }

        SrLogger.LogWarning("UPnP: All port mapping attempts failed.");
        return 0;
    }

    private static void StartRefresh(INatDevice device, ushort port)
    {
        lock (RefreshLock)
        {
            refreshDevice = device;
            refreshPort = port;
            refreshTimer?.Dispose();
            refreshTimer = new Timer(_ => RefreshMapping(), null,
                TimeSpan.FromSeconds(RefreshIntervalSeconds),
                TimeSpan.FromSeconds(RefreshIntervalSeconds));
        }
    }

    private static void RefreshMapping()
    {
        try
        {
            if (!Main.Server.IsRunning || Main.Server.Port != refreshPort)
            {
                StopRefresh();
                return;
            }

            var mapping = new Mapping(Protocol.Udp, refreshPort, refreshPort, 0, string.Empty);
            refreshDevice?.CreatePortMapAsync(mapping, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"UPnP: Failed to refresh port mapping: {ex.Message}");
        }
    }

    private static HashSet<IPAddress> GetGatewayAddresses()
    {
        var gateways = new HashSet<IPAddress>();
        try
        {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            foreach (var gateway in iface.GetIPProperties().GatewayAddresses)
                gateways.Add(gateway.Address);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"UPnP: Failed to enumerate gateways: {ex.Message}");
        }
        return gateways;
    }

    private static bool IsUsableIp(IPAddress? ip) =>
        ip != null &&
        !ip.Equals(IPAddress.Any) &&
        !ip.Equals(IPAddress.IPv6Any) &&
        !ip.Equals(IPAddress.Loopback);

    private static IPAddress? TryGetExternalIp()
    {
        try
        {
            var response = IpApiClient.GetStringAsync("https://api.ipify.org").GetAwaiter().GetResult();
            return IPAddress.TryParse(response.Trim(), out var ip) ? ip : null;
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"UPnP: External API request failed: {ex.Message}");
            return null;
        }
    }
}