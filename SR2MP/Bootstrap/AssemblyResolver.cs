using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SR2MP.Bootstrap;

internal static class AssemblyResolver
{
    private static bool isInstalled;

    internal static void Install()
    {
        if (isInstalled)
            return;

        AppDomain.CurrentDomain.AssemblyResolve += ResolveBundledAssembly;
        isInstalled = true;
    }

    private static Assembly? ResolveBundledAssembly(object? sender, ResolveEventArgs args)
    {
        var requestedName = new AssemblyName(args.Name).Name;
        if (string.IsNullOrEmpty(requestedName))
            return null;

        var alreadyLoaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(asm => string.Equals(asm.GetName().Name, requestedName, StringComparison.OrdinalIgnoreCase));
        if (alreadyLoaded != null)
            return alreadyLoaded;

        string? resourceName = requestedName switch
        {
            "LiteNetLib" => "SR2MP.LiteNetLib.dll",
            "SharpOpenNat" => "SR2MP.SharpOpenNat.dll",
            "DiscordRPC" => "SR2MP.DiscordRPC.dll",
            _ => null
        };

        if (resourceName == null)
            return null;

        var assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return null;

        byte[] bytes = new byte[stream.Length];
        _ = stream.Read(bytes, 0, bytes.Length);
        return Assembly.Load(bytes);
    }
}
