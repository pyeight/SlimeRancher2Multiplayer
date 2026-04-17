using JetBrains.Annotations;

namespace SR2MP.Packets.Utils;

/// <summary>
/// An interface that represents a network object.
/// </summary>
/// <remarks>All net object types MUST have a parameter-less constructor! Either make the type a struct (which always has a parameter-less constructor), or at least declare a parameter-less constructor for classes!</remarks>
[PublicApi]
public interface INetObject
{
    /// <summary>
    /// Serialises the network object to a packet writer.
    /// </summary>
    /// <param name="writer">The packet writer to serialise the network object to.</param>
    void Serialise(PacketWriter writer);

    /// <summary>
    /// Deserialises the network object from a packet reader.
    /// </summary>
    /// <param name="reader">The packet reader to deserialise the network object from.</param>
    void Deserialise(PacketReader reader);
}