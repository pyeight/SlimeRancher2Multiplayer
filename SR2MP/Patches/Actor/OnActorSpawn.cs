using System.Collections;
using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using MelonLoader;
using SR2MP.Components.Actor;
using SR2MP.Packets.Utils;
using UnityEngine.SocialPlatforms;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(InstantiationHelpers), nameof(InstantiationHelpers.InstantiateActor))]
public static class OnActorSpawn
{
    private static int cachedPlayerIndex = -1;

    private static int GetLocalPlayerIndex()
    {
        if (cachedPlayerIndex != -1)
        {
            return cachedPlayerIndex;
        }

        int i = 0;

        foreach (var player in playerObjects)
        {
            if (LocalID == player.Key)
            {
                cachedPlayerIndex = i;
                return i;
            }

            i++;
        }

        return 0;
    }

    private static IEnumerator SpawnOverNetwork(int actorType, byte sceneGroup, GameObject actor)
    {
        yield return null;
        yield return null;

        var id = actor.GetComponent<IdentifiableActor>().GetActorId();

        actorManager.Actors.Add(id.Value, actor.GetComponent<IdentifiableActor>()._model);

        var packet = new ActorSpawnPacket()
        {
            Type = (byte)PacketType.ActorSpawn,
            ActorType = actorType,
            SceneGroup = sceneGroup,
            ActorId = id,
            Position = actor.transform.position,
            Rotation = actor.transform.rotation,
        };

        Main.SendToAllOrServer(packet);
    }

    public static void Prefix()
    {
        var nextId = SceneContext.Instance.GameModel._actorIdProvider._nextActorId;
        var playerOffset = GetLocalPlayerIndex() * 10000;

        if (nextId < 10000)
        {
            SceneContext.Instance.GameModel._actorIdProvider._nextActorId += playerOffset;
        }
    }

    public static void Postfix(
        GameObject __result,
        GameObject original,
        SceneGroup sceneGroup,
        Vector3 position,
        Quaternion rotation,
        bool nonActorOk = false,
        SlimeAppearance.AppearanceSaveSet appearance = SlimeAppearance.AppearanceSaveSet.NONE,
        SlimeAppearance.AppearanceSaveSet secondAppearance = SlimeAppearance.AppearanceSaveSet.NONE)
    {
        if (handlingPacket) return;
        __result.AddComponent<NetworkActor>().LocallyOwned = true;

        var actorType = actorManager.GetPersistentID(original.GetComponent<Identifiable>().identType);
        var sceneGroupId = GameContext.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(sceneGroup);

        MelonCoroutines.Start(SpawnOverNetwork(actorType, (byte)sceneGroupId, __result));
    }
}