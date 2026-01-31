using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SharpOpenNat;

namespace SR2MP.Shared.Utils;

// UPnP hole punching! Unfortunately not usable if the user is behind CGNAT
public sealed class AutoHostResult
{
    private AutoHostResult(bool success, ushort port, IPAddress? externalIp, string joinCode, string errorMessage)
    {
        Success = success;
        Port = port;
        ExternalIp = externalIp;
        JoinCode = joinCode;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }
    public ushort Port { get; }
    public IPAddress? ExternalIp { get; }
    public string JoinCode { get; }
    public string ErrorMessage { get; }

    public static AutoHostResult Failure(string message) => new(false, 0, null, string.Empty, message);
    public static AutoHostResult SuccessResult(ushort port, IPAddress externalIp, string joinCode) => new(true, port, externalIp, joinCode, string.Empty);
}

public static class AutoHostUtility
{
    private const int MappingTtlSeconds = 15 * 60; // 15 minute UPnP TTL
    private const int RefreshIntervalSeconds = 3 * 60; // We refresh it every 3 minutes
    private static readonly HttpClient IpApiClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private static readonly object refreshLock = new();
    private static Timer? refreshTimer;
    private static INatDevice? refreshDevice;
    private static ushort refreshPort;

    public static void BeginAutoHost(Action<AutoHostResult> onCompleted)
    {
        if (onCompleted == null)
            throw new ArgumentNullException(nameof(onCompleted));

        Task.Run(() =>
        {
            StopRefresh();
            var result = RunAutoHost();
            MainThreadDispatcher.Enqueue(() => onCompleted(result));
        });
    }

    private static AutoHostResult RunAutoHost()
    {
        SrLogger.LogMessage("UPnP: Starting discovery...", SrLogTarget.Both);
        try
        {
            using var discoveryCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var device = OpenNat.Discoverer.DiscoverDeviceAsync(PortMapper.Upnp, discoveryCts.Token)
                .GetAwaiter().GetResult();
            SrLogger.LogMessage("UPnP: Device discovered.", SrLogTarget.Both);

            using var mapCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var random = new System.Random();
            ushort selectedPort = 0;
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                // IANA private port range (so we won't step on anyones feet)
                var port = (ushort)random.Next(49152, 65536);
                SrLogger.LogMessage($"UPnP: Attempt {attempt} mapping UDP port {port}.", SrLogTarget.Both);
                try
                {
                    var mapping = new Mapping(Protocol.Udp, port, port, MappingTtlSeconds, $"SR2MP_{port}");
                    device.CreatePortMapAsync(mapping, mapCts.Token).GetAwaiter().GetResult();
                    selectedPort = port;
                    SrLogger.LogMessage($"UPnP: Port {port} mapped successfully.", SrLogTarget.Both);
                    break;
                }
                catch (Exception ex)
                {
                    SrLogger.LogWarning($"UPnP: Failed to map port on attempt {attempt}: {ex.Message}", SrLogTarget.Both);
                }
            }

            if (selectedPort == 0)
            {
                return AutoHostResult.Failure("UPnP failed to map a port.");
            }

            IPAddress? externalIp = null;
            try
            {
                externalIp = device.GetExternalIPAsync(discoveryCts.Token).GetAwaiter().GetResult();
                if (externalIp == null)
                    throw new InvalidOperationException("External IP was null.");
                SrLogger.LogMessage($"UPnP: External IP detected: {externalIp}", SrLogTarget.Both);
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"UPnP: Failed to get external IP: {ex.Message}", SrLogTarget.Both);
            }

            var apiIp = TryGetExternalIpFromApi();
            if (apiIp != null)
                SrLogger.LogMessage($"UPnP: External IP fetched via API: {apiIp}", SrLogTarget.Both);

            if (externalIp != null && apiIp != null && !externalIp.Equals(apiIp))
            {
                SrLogger.LogWarning($"UPnP: Router WAN IP ({externalIp}) differs from IP API ({apiIp}). You may be behind CGNAT or a proxy, external connections might fail.", SrLogTarget.Both);
            }

            if (externalIp == null && apiIp != null)
            {
                externalIp = apiIp;
            }

            if (externalIp == null)
            {
                return AutoHostResult.Failure("UPnP mapped the port but could not fetch external IP.");
            }

            var joinCode = JoinCode.Encode(externalIp, selectedPort);
            StartRefresh(device, selectedPort);
            return AutoHostResult.SuccessResult(selectedPort, externalIp, joinCode);
        }
        catch (NatDeviceNotFoundException ex)
        {
            SrLogger.LogWarning($"UPnP: No device found: {ex.Message}", SrLogTarget.Both);
            return AutoHostResult.Failure("UPnP is not available on this network.");
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"UPnP: Failed with error: {ex.Message}", SrLogTarget.Both);
            return AutoHostResult.Failure($"UPnP failed: {ex.Message}.");
        }
    }

    private static void StartRefresh(INatDevice device, ushort port)
    {
        lock (refreshLock)
        {
            refreshDevice = device;
            refreshPort = port;

            refreshTimer?.Dispose();
            refreshTimer = new Timer(_ =>
            {
                try
                {
                    if (!Main.Server.IsRunning() || Main.Server.Port != refreshPort)
                    {
                        StopRefresh();
                        return;
                    }

                    var mapping = new Mapping(Protocol.Udp, refreshPort, refreshPort, MappingTtlSeconds, $"SR2MP_{refreshPort}");
                    refreshDevice?.CreatePortMapAsync(mapping, CancellationToken.None).GetAwaiter().GetResult();
                    SrLogger.LogMessage($"UPnP: Refreshed port mapping for {refreshPort}.", SrLogTarget.Both);
                }
                catch (Exception ex)
                {
                    SrLogger.LogWarning($"UPnP: Failed to refresh port mapping: {ex.Message}", SrLogTarget.Both);
                }
            }, null, TimeSpan.FromSeconds(RefreshIntervalSeconds), TimeSpan.FromSeconds(RefreshIntervalSeconds));
        }
    }

    private static void StopRefresh()
    {
        lock (refreshLock)
        {
            refreshTimer?.Dispose();
            refreshTimer = null;
            refreshDevice = null;
            refreshPort = 0;
        }
    }

    private static IPAddress? TryGetExternalIpFromApi()
    {
        try
        {
            var response = IpApiClient.GetStringAsync("https://api.ipify.org").GetAwaiter().GetResult();
            var ipString = response.Trim();
            return IPAddress.TryParse(ipString, out var ip) ? ip : null;
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"UPnP: External IP API failed: {ex.Message}", SrLogTarget.Both);
            return null;
        }
    }
}
