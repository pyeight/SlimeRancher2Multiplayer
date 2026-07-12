using System.Collections;
using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorDestroy)]
internal sealed class ActorDestroyHandler : BasePacketHandler<ActorDestroyPacket>
{
    protected override bool Handle(ActorDestroyPacket packet, IPEndPoint? _)
    {
        //if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
        //{
        //    SrLogger.LogDebug($"Actor {packet.ActorId.Value} doesn't exist (already destroyed?)", SrLogTarget.Both);
        //    return false;
        //}

        if (GameState.TryGetIdentifiableModel(packet.ActorId, out var actor))
        {
            // Remove the drone station if it's a drone
            if (actor.TryCast<DroneStationGadgetModel>() != null)
                NetworkDroneManager.RemoveStationDrone(packet.ActorId);

            GameState.identifiables.Remove(packet.ActorId);
            GameState.identifiablesByIdent[actor.ident].Remove(actor);
            GameState.DestroyIdentifiableModel(actor);
            ActorManager.Actors.Remove(actor.actorId.Value);

            HandlingPacket = true;

            StartCoroutine(DestroyActor(actor));

            HandlingPacket = false;
        }

        return true;
    }

    private static IEnumerator DestroyActor(IdentifiableModel actor)
    {
        for (int i = 0; i < 20; i++)
        {
            try
            {
                var obj = actor.GetGameObject();

                if (obj)
                {
                    Destroyer.DestroyAny(obj, "SR2MP.ActorDestroyHandler");
                    yield break;
                }
            }
            catch
            {
                // ignored
            }

            yield return null; // wait one frame
        }
    }
}