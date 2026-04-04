namespace SR2MP.Packets.Utils;

/// <summary>
/// An enum that denotes the type of number to serialise a count as.
/// </summary>
public enum CountType : byte
{
    /// <summary>
    /// Serialise as a byte.
    /// </summary>
    Byte,

    /// <summary>
    /// Serialise as a ushort.
    /// </summary>
    UShort,

    /// <summary>
    /// Serialise as a uint.
    /// </summary>
    UInt,

    /// <summary>
    /// Serialise as a packed uint.
    /// </summary>
    VarUInt
}