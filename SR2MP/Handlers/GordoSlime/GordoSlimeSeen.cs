using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.GordoSlime;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoSlimeSeen)]
internal sealed class GordoSlimeSeenHandler : BasePacketHandler<GordoSlimeSeenPacket>
{
    protected override bool Handle(GordoSlimeSeenPacket packet, IPEndPoint? _)
    {
        if (!GameState.gordos.TryGetValue(packet.ID, out var gordoSlimeModel))
        {
            gordoSlimeModel = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoSeen = true,
                gameObj = null,
                targetCount = 50
            };

            GameState.gordos.Add(packet.ID, gordoSlimeModel);
            return true;
        }
        
        gordoSlimeModel.gordoSeen = true;

        return true;
    }
}
