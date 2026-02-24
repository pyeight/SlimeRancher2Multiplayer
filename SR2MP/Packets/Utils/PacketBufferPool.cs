using SR2MP.Shared.Utils;

namespace SR2MP.Packets.Utils;

public static class PacketBufferPool
{
    public static PacketReader GetReader(byte[] data, int size = -1, bool rented = false)
    {
        var reader = RecyclePool<PacketReader>.Borrow();
        reader.SetBuffer(data, size, rented);
        return reader;
    }

    public static PacketWriter GetWriter(int initialCapacity = 256)
    {
        var writer = RecyclePool<PacketWriter>.Borrow();
        writer.Reset(initialCapacity);
        return writer;
    }

    public static void Return(PacketReader reader) => RecyclePool<PacketReader>.Return(reader);

    public static void Return(PacketWriter writer) => RecyclePool<PacketWriter>.Return(writer);
}