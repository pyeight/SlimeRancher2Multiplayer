using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct PlayerFXPacket : IPacket
{
    public enum PlayerFXType : byte
    {
        None,
        VacReject,
        VacHold,
        VacAccept,
        WalkTrail,
        VacShoot,
        VacShootEmpty,
        WaterSplash,
        VacSlotChange,
        VacRunning,
        VacRunningStart,
        VacRunningEnd,
        VacShootSound,
    }
    
    public byte Type { get; set; }
    public PlayerFXType FX { get; set; }
    
    public Vector3 Position { get; set; }
    
    public string Player { get; set; }
    
    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteEnum(FX);
        if (!IsPlayerSoundDictionary[FX])
            writer.WriteVector3(Position);
        else
            writer.WriteString(Player);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        FX = reader.ReadEnum<PlayerFXType>();
        if (!IsPlayerSoundDictionary[FX])
            Position = reader.ReadVector3();
        else
            Player = reader.ReadString();
    }
}