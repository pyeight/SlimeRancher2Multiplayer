using System.IO;
using System.IO.Compression;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Shared;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.SaveData)]
    public class SaveHandler : BaseClientPacketHandler
    {
        public SaveHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(byte[] data)
        {
            MelonLoader.MelonLogger.Msg("Received Save Data Packet.");

            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<SaveDataPacket>();

            // 1. Decompress
            byte[] decompressedData;
            using (var inputStream = new MemoryStream(packet.Data))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                decompressedData = outputStream.ToArray();
            }

            MelonLoader.MelonLogger.Msg($"Decompressed save data: {decompressedData.Length} bytes. Loading...");

            // 2. Load into Game
            // Use a temporary game/save name for MP to avoid overwriting main saves? 
            // GitHub code uses "SR2MP" for both GameName and SaveName
            
            var memoryStream = new Il2CppSystem.IO.MemoryStream(decompressedData);
            memoryStream.Seek(0, Il2CppSystem.IO.SeekOrigin.Begin);

            var gsi = new Il2CppMonomiPark.SlimeRancher.GameSaveIdentifier() 
            { 
                GameName = "SR2MP_Client", 
                SaveName = "SR2MP_Save" 
            };

            var autoSaveDirector = GameContext.Instance.AutoSaveDirector;
            if (autoSaveDirector != null)
            {
                 // BeginLoad takes (GameSaveIdentifier, Stream)
                 // Based on Logic, we just feed it the stream and identifier.
                 autoSaveDirector.BeginLoad(gsi, memoryStream);
                 MelonLoader.MelonLogger.Msg("World load initiated.");
            }
            else
            {
                MelonLoader.MelonLogger.Error("AutoSaveDirector is null!");
            }
        }
    }
}
