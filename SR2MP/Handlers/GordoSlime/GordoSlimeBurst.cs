using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.GordoSlime;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoSlimeBurst)]
internal sealed class GordoSlimeBurstHandler : BasePacketHandler<GordoSlimeBurstPacket>
{
    protected override bool Handle(GordoSlimeBurstPacket packet, IPEndPoint? _)
    {
        if (!GameState.gordos.TryGetValue(packet.ID, out var gordoSlime))
        {
            gordoSlime = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoSeen = false,
                gameObj = null,
                targetCount = 50
            };

            GameState.gordos.Add(packet.ID, gordoSlime);
        }

        NetworkGordoSlimeManager.MarkPopped(packet.ID);

        NetworkGordoSlimeManager.MarkRewarded(packet.ID);

        HandlingPacket = true;
        gordoSlime.GordoEatenCount = gordoSlime.targetCount;

        try
        {
            if (gordoSlime.gameObj)
                gordoSlime.gameObj.GetComponent<GordoEat>().ImmediateReachedTarget();
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"GordoSlimeBurst: visual burst failed for Gordo Slime {packet.ID}: {ex.Message}");
        }

        HandlingPacket = false;
        
        gordoSlime.gordoSeen = false;

        return true;
    }
}