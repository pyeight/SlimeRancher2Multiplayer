using JetBrains.Annotations;

namespace SR2MP.Api;

/// <summary>
/// Defines the network architecture requirements for a mod.
/// </summary>
[PublicApi, Flags]
public enum ModSide : byte
{
    /// <summary>Runs on neither side.</summary>
    /// <remarks>Exists for convention, DO NOT USE!</remarks>
    None = 0,

    /// <summary>Runs locally.</summary>
    Local = 1 << 0,

    /// <summary>Runs only on the host.</summary>
    Host = 1 << 1,

    /// <summary>Required on both the host and all connected clients.</summary>
    Shared = Local | Host
}