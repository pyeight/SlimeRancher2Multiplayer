using SR2MP.Packets.Utils;
using UnityEngine;
using Il2CppMonomiPark.SlimeRancher.DataModel;

namespace SR2MP.Packets.Shared
{
    public class DecorationPacket : IPacket
    {
        public PacketType Type => PacketType.Decoration;

        public long DecorationId; // Using long for identifying via GetHashCode or similar if generic ID is not available
        public IdentifiableType TypeId;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsRemoval;

        public DecorationPacket() { }

        public DecorationPacket(long id, IdentifiableType typeId, Vector3 pos, Quaternion rot, bool isRemoval)
        {
            DecorationId = id;
            TypeId = typeId;
            Position = pos;
            Rotation = rot;
            IsRemoval = isRemoval;
        }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteLong(DecorationId);
            writer.WriteBool(IsRemoval);
            
            if (!IsRemoval)
            {
                writer.WriteString(TypeId != null ? TypeId.name : "");
                writer.WriteVector3(Position);
                writer.WriteQuaternion(Rotation);
            }
        }

        public void Deserialise(PacketReader reader)
        {
            DecorationId = reader.ReadLong();
            IsRemoval = reader.ReadBool();

            if (!IsRemoval)
            {
                string typeName = reader.ReadString();
                if (!string.IsNullOrEmpty(typeName))
                {
                    var allTypes = Resources.FindObjectsOfTypeAll<IdentifiableType>();
                    foreach (var type in allTypes)
                    {
                        if (type.name == typeName)
                        {
                            TypeId = type;
                            break;
                        }
                    }
                }

                Position = reader.ReadVector3();
                Rotation = reader.ReadQuaternion();
            }
        }
    }
}
