namespace SR2MP.Packets.Utils;

public sealed class PacketStream<T> : Stream where T : PacketBuffer
{
    private readonly T _buffer;

    public override bool CanSeek => true;
    public override bool CanRead => _buffer.BufferType == BufferType.Reader;
    public override bool CanWrite => _buffer.BufferType == BufferType.Writer;
    public override long Length => _buffer.DataSize;
    public override long Position
    {
        get => _buffer.Position;
        set => _buffer.SetCursor((int)value);
    }

    public PacketStream(T buffer)
        => _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

    public override void Flush() => _buffer.EndPackingBools();

    public override long Seek(long offset, SeekOrigin origin)
    {
        var current = origin switch
        {
            SeekOrigin.Begin => 0,
            SeekOrigin.End => Length - 1,
            _ => Position
        };
        return Position = current + offset;
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_buffer is not PacketReader reader)
            throw new NotSupportedException("This stream does not support reading.");

        var bytesAvailable = (int)(Length - Position);
        var bytesToRead = Math.Min(count, bytesAvailable);

        if (bytesToRead <= 0)
            return 0;

        reader.ReadToSpan(buffer.AsSpan(offset, bytesToRead));
        return bytesToRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_buffer is not PacketWriter writer)
            throw new NotSupportedException("This stream does not support writing.");

        writer.WriteSpan(buffer.AsSpan(offset, count));
    }
}