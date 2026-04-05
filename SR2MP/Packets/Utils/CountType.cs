using JetBrains.Annotations;

namespace SR2MP.Packets.Utils;

/// <summary>
/// An enum that denotes the type of number to serialise a count as.
/// </summary>
[PublicAPI]
public enum CountType : byte
{
    /// <summary>
    /// Serialise as a byte.
    /// </summary>
    Byte = 0,

    /// <summary>
    /// Serialise as a ushort.
    /// </summary>
    UShort = 1,

    /// <summary>
    /// Serialise as a packed uint.
    /// </summary>
    VarUInt = 2
}