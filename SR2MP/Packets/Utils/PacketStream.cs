namespace SR2MP.Packets.Utils;

public sealed class PacketStream : Stream
{
    private readonly PacketWriter _writer;

    public PacketStream(PacketWriter writer)
        => _writer = writer ?? throw new ArgumentNullException(nameof(writer));

    public override void Write(ReadOnlySpan<byte> buffer) => _writer.WriteSpan(buffer);

    public override void Write(byte[] buffer, int offset, int count) => _writer.WriteSpan(buffer.AsSpan(offset, count));

    // Required Stream boilerplate
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _writer.Position;
    public override long Position { get => _writer.Position; set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}