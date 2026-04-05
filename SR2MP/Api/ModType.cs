using JetBrains.Annotations;

namespace SR2MP.Api;

/// <summary>
/// An enum that denotes the type of mod.
/// </summary>
[PublicAPI]
public enum ModType : byte
{
    /// <summary>
    /// The mod is only used on the client's machine.
    /// </summary>
    ClientOnly,

    /// <summary>
    /// The mod is only used on the host's machine.
    /// </summary>
    HostOnly,

    /// <summary>
    /// The mod is used on both the client's and the host's machines.
    /// </summary>
    ClientAndHost,
}