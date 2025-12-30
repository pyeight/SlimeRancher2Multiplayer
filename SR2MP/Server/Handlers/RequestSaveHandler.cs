using System.IO;
using System.IO.Compression;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Shared;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using UnityEngine;

namespace SR2MP.Server.Handlers
{
    [PacketHandler((byte)PacketType.RequestSave)]
    public class RequestSaveHandler : BasePacketHandler
    {
        public RequestSaveHandler(NetworkManager networkManager, ClientManager clientManager) 
            : base(networkManager, clientManager) { }

        public override void Handle(byte[] data, string clientIdentifier)
        {
            MelonLoader.MelonLogger.Msg("Received Save Request. preparing save data...");
            
            // 1. Force a save (Host side)
            // Note: This might cause a hitch.
            var autoSaveDirector = GameContext.Instance.AutoSaveDirector;
            if (autoSaveDirector == null) return;

            autoSaveDirector.SaveGame();
            
            // 2. Get the save data
            var save = autoSaveDirector.GetSaveToContinue();
            if (save == null)
            {
                MelonLoader.MelonLogger.Error("Could not get save to continue!");
                return;
            }

            // 3. Read into IL2CPP memory stream
            var il2cppStream = new Il2CppSystem.IO.MemoryStream();
            autoSaveDirector._storageProvider.GetGameData(save.SaveName, il2cppStream);
            il2cppStream.Seek(0, Il2CppSystem.IO.SeekOrigin.Begin);

            // 4. Convert to byte array
            byte[] rawData = il2cppStream.ToArray();

            // 5. Compress
            byte[] compressedData;
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    gzipStream.Write(rawData, 0, rawData.Length);
                }
                compressedData = outputStream.ToArray();
            }

            MelonLoader.MelonLogger.Msg($"Sending compressed save data ({compressedData.Length} bytes) to client...");

            // 6. Send back
            var packet = new SaveDataPacket(compressedData);
            Main.Server.SendToClient(packet, clientIdentifier);
        }
    }
}
