using System.Collections.Concurrent;

namespace SR2MP.Packets.Utils;

public static class PacketBufferPool
{
    // Thread-safe collections to hold our pooled instances
    private static readonly ConcurrentBag<PacketReader> ReaderPool = new();
    private static readonly ConcurrentBag<PacketWriter> WriterPool = new();

    public static PacketReader GetReader(byte[] data, int size = -1, bool rented = false)
    {
        if (!ReaderPool.TryTake(out var reader))
            return new(data, size, rented);

        reader.SetBuffer(data, size, rented);
        return reader;
    }

    public static PacketWriter GetWriter(int initialCapacity = 256)
    {
        if (!WriterPool.TryTake(out var writer))
            return new(initialCapacity);

        writer.Reset(initialCapacity);
        return writer;
    }

    public static void Return(PacketReader reader)
    {
        if (reader == null)
            return;

        reader.Dispose(); // Ensure any resources are cleaned up before returning to the pool
        ReaderPool.Add(reader);
    }

    public static void Return(PacketWriter writer)
    {
        if (writer == null)
            return;

        writer.Dispose(); // Ensure any resources are cleaned up before returning to the pool
        WriterPool.Add(writer);
    }
}