namespace SR2MP.Packets.Utils;

/// <summary>
/// Write delegate.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <param name="writer">The writer to write to.</param>
/// <param name="value">The value to write.</param>
public delegate void WriteDel<in T>(PacketWriter writer, T value);

/// <summary>
/// Read delegate.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <param name="reader">The reader to read the value from.</param>
/// <returns>The read value.</returns>
public delegate T ReadDel<out T>(PacketReader reader);