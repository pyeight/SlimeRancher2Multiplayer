using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

/// <summary>
/// When working with packets, please add 'C' if it's a C2S packet or 'S' if it's a S2C packet <br/>
/// For example, <c>SConnectAckPacket</c> for when the server responds to the client's <c>CConnectPacket</c>.
/// <br/> <br/>
/// Do not add anything at the start if the packet should go 2 ways.
/// </summary>
public struct EmptyPacket : IPacket
{
    public byte Type { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
    }
}