using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Packets.Loading;

internal sealed class InitialDroneResourcesPacket : IPacket
{
    internal sealed class SceneEntries
    {
        public int Scene;
        public List<DroneResourcePacket.DroneResourceEntry> Entries = new();
    }

    public List<SceneEntries> Scenes = new();

    public PacketType Type => PacketType.InitialDroneResources;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(Scenes.Count);

        foreach (var scene in Scenes)
        {
            writer.WritePackedInt(scene.Scene);
            writer.WritePackedInt(scene.Entries.Count);

            foreach (var entry in scene.Entries)
                entry.Serialise(writer);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        var sceneCount = reader.ReadPackedInt();
        Scenes = new List<SceneEntries>(sceneCount);

        for (var i = 0; i < sceneCount; i++)
        {
            var scene = new SceneEntries { Scene = reader.ReadPackedInt() };

            var entryCount = reader.ReadPackedInt();
            scene.Entries = new List<DroneResourcePacket.DroneResourceEntry>(entryCount);

            for (var j = 0; j < entryCount; j++)
            {
                var entry = new DroneResourcePacket.DroneResourceEntry();
                entry.Deserialise(reader);
                scene.Entries.Add(entry);
            }

            Scenes.Add(scene);
        }
    }
}
