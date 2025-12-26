using Il2Cpp;
using Mono.Cecil;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

public class PlayerUpgradesLoadHandler : BaseClientPacketHandler
{
    public PlayerUpgradesLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager)
    {
    }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<UpgradesPacket>();

        var upgradeId = 0;
        var upgradesCLient = Resources.FindObjectsOfTypeAll<UpgradeDefinition>();
        foreach (var upgradeLvl in packet.Upgrades)
        {
            SceneContext.Instance.PlayerState._model.upgradeModel.SetUpgradeLevel(upgradesCLient[upgradeId],
                upgradeLvl);
            upgradeId++;
        }
    }
}