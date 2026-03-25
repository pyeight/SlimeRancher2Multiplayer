using SR2MP.Packets.Api;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Utils;

internal delegate void PacketWriterDelegate<in T>(PacketWriter writer, T state);

internal static class SerialiseApiPacket<T> where T : ICustomPacket
{
    public static readonly PacketWriterDelegate<(ApiPacket Packet, T Data)> Func = (writer, state) =>
    {
        writer.WritePacket(state.Packet);
        writer.WriteCustomPacket(state.Data);
    };
}

internal static class SerialiseInternalPacket<T> where T : IPacket
{
    public static readonly PacketWriterDelegate<T> Func = (writer, packet) => writer.WritePacket(packet);
}