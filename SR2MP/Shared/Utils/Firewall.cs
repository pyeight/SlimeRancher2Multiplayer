using System.Diagnostics;

namespace SR2MP.Shared.Utils;

public static class Firewall
{
    private static MelonLoader.MelonPreferences_Entry<string>? firewallEntries;

    internal static void Initialize(MelonLoader.MelonPreferences_Entry<string> firewallEntry) { firewallEntries = firewallEntry; }

    internal static void AddException(ushort port)
    {
        // todo: review
        return;
        if (!OperatingSystem.IsWindows())
        {
            SrLogger.LogWarning("Firewall: Could not add a Firewall exception, you may have to add it on your own.", SrLogTarget.Both);
            return;
        }

        var path = Process.GetCurrentProcess().MainModule?.FileName;

        RunShell($"advfirewall firewall add rule name=\"SR2MP UDP In {port}\" dir=in action=allow protocol=UDP localport={port} program=\"{path}\" enable=yes");
        RunShell($"advfirewall firewall add rule name=\"SR2MP UDP Out {port}\" dir=out action=allow protocol=UDP localport={port} program=\"{path}\" enable=yes");
        AddPort(port);
        SrLogger.LogMessage($"Firewall: Added UDP exceptions for port {port}.", SrLogTarget.Both);
    }

    internal static void RemoveException(ushort port)
    {
        if (!OperatingSystem.IsWindows())
            return;

        RunShell($"advfirewall firewall delete rule name=\"SR2MP UDP In {port}\"");
        RunShell($"advfirewall firewall delete rule name=\"SR2MP UDP Out {port}\"");
        RemovePort(port);
        SrLogger.LogMessage($"Firewall: Removed UDP exceptions for port {port}.", SrLogTarget.Both);
    }

    internal static void RemoveAllExceptions()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var ports = GetExceptionPorts();
        if (ports.Count == 0)
        {
            SrLogger.LogMessage("Firewall: No tracked exceptions to remove.", SrLogTarget.Both);
            return;
        }

        SrLogger.LogMessage($"Firewall: Removing {ports.Count} tracked exception(s): {string.Join(", ", ports)}", SrLogTarget.Both);
        foreach (var port in ports)
        {
            RunShell($"advfirewall firewall delete rule name=\"SR2MP UDP In {port}\"");
            RunShell($"advfirewall firewall delete rule name=\"SR2MP UDP Out {port}\"");
        }

        ClearPorts();
    }

    private static HashSet<ushort> GetExceptionPorts()
    {
        if (firewallEntries == null) return new HashSet<ushort>();
        return firewallEntries.Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => ushort.TryParse(s.Trim(), out var p) ? p : (ushort)0)
            .Where(p => p != 0)
            .ToHashSet();
    }

    private static void AddPort(ushort port)
    {
        if (firewallEntries == null) return;
        var ports = GetExceptionPorts();
        ports.Add(port);
        SavePorts(ports);
    }

    private static void RemovePort(ushort port)
    {
        if (firewallEntries == null) return;
        var ports = GetExceptionPorts();
        ports.Remove(port);
        SavePorts(ports);
    }

    private static void ClearPorts()
    {
        if (firewallEntries == null) return;
        firewallEntries.Value = string.Empty;
        firewallEntries.Category.SaveToFile();
    }

    private static void SavePorts(HashSet<ushort> ports)
    {
        if (firewallEntries == null) return;
        firewallEntries.Value = string.Join(",", ports);
        firewallEntries.Category.SaveToFile();
    }

    private static void RunShell(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("netsh", args)
            {
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            var process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Firewall: Failed to execute: {ex.Message}", SrLogTarget.Both);
        }
    }
}