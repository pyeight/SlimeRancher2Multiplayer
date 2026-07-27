using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.GordoSlime;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoSlimeReward)]
internal sealed class GordoSlimeRewardHandler : BasePacketHandler<GordoSlimeRewardPacket>
{
    protected override bool Handle(GordoSlimeRewardPacket packet, IPEndPoint? _)
    {
        try
        {
            if (!GameState.gordos.TryGetValue(packet.GordoSlimeId, out var gordoSlimeModel) || !gordoSlimeModel.gameObj)
                return true;

            var rewardsBase = gordoSlimeModel.gameObj.GetComponent<GordoRewardsBase>();
            if (!rewardsBase) return true;

            var rewards = rewardsBase.TryCast<GordoRewards>();
            if (rewards == null) return true;

            var rewardPrefabs = rewards.RewardPrefabs;
            if (rewardPrefabs == null || packet.PrefabIndex < 0 || packet.PrefabIndex >= rewardPrefabs.Length)
                return true;

            var prefab = rewardPrefabs[packet.PrefabIndex];
            if (!prefab) return true;

            HandlingPacket = true;
            var instance = Object.Instantiate(prefab);
            instance.transform.SetPositionAndRotation(packet.Position, packet.Rotation);
            HandlingPacket = false;
        }
        catch (Exception ex)
        {
            HandlingPacket = false;
            SrLogger.LogWarning($"GordoSlimeRewardHandler: failed to spawn reward for Gordo Slime {packet.GordoSlimeId}: {ex.Message}");
        }

        return true;
    }
}
