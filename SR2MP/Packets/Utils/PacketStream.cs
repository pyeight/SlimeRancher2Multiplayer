// namespace SR2MP.Packets.Utils;

// public abstract class PacketStream<T> : Stream where T : PacketBuffer
// {
//     protected readonly T _buffer;
//     private readonly bool _leaveOpen;

//     public sealed override bool CanSeek => true;
//     public sealed override long Length => _buffer.DataSize;

//     public sealed override long Position
//     {
//         get => _buffer.Position;
//         set => _buffer.SetCursor(value);
//     }

//     public override bool CanRead => false;
//     public override bool CanWrite => false;

//     protected PacketStream(T buffer, bool leaveOpen = true)
//     {
//         _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
//         _leaveOpen = leaveOpen;
//     }

//     public sealed override void Flush() => _buffer.EndPackingBools();

//     public sealed override long Seek(long offset, SeekOrigin origin) => Position = offset + (origin switch
//     {
//         SeekOrigin.Begin => 0,
//         SeekOrigin.End => Length,
//         _ => Position
//     });

//     public sealed override void SetLength(long value) => throw new NotSupportedException();

//     public sealed override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

//     public sealed override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

//     protected sealed override void Dispose(bool disposing)
//     {
//         if (!_leaveOpen)
//             _buffer.Dispose();

//         base.Dispose(disposing);
//     }

//     public override int Read(Span<byte> buffer)
//         => throw new NotSupportedException("This stream does not support reading.");

//     public override void Write(ReadOnlySpan<byte> buffer)
//         => throw new NotSupportedException("This stream does not support writing.");
// }

// public sealed class PacketWriterStream : PacketStream<PacketWriter>
// {
//     public override bool CanWrite => true;

//     public PacketWriterStream(PacketWriter buffer, bool leaveOpen = true)
//         : base(buffer, leaveOpen) { }

//     public override void Write(ReadOnlySpan<byte> buffer) => _buffer.WriteSpan(buffer);
// }

// public sealed class PacketReaderStream : PacketStream<PacketReader>
// {
//     public override bool CanRead => true;

//     public PacketReaderStream(PacketReader buffer, bool leaveOpen = true)
//         : base(buffer, leaveOpen) { }

//     public override int Read(Span<byte> buffer)
//     {
//         var bytesAvailable = (int)(Length - Position);
//         var bytesToRead = Math.Min(buffer.Length, bytesAvailable);

//         if (bytesToRead <= 0)
//             return 0;

//         _buffer.ReadToSpan(buffer[..bytesToRead]);
//         return bytesToRead;
//     }
// }