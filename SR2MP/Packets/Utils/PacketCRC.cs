namespace SR2MP.Packets.Utils;

public static class PacketCRC
{
    public static ushort Compute(byte[] data, int offset, int length)
    {
        ushort crc = 0x0000;
        for (var i = offset; i < offset + length; i++)
        {
            crc ^= (ushort)(data[i] << 8);
            for (var j = 0; j < 8; j++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x8005) : (ushort)(crc << 1);
        }
        return crc;
    }

    public static ushort Compute(ArraySegment<byte> segment)
        => Compute(segment.Array!, segment.Offset, segment.Count);
}