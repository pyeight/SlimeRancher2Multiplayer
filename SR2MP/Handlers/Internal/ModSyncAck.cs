using System.Net;
using SR2MP.Packets.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Internal;

[PacketHandler((byte)PacketType.ModSyncAck, HandlerType.Server)]
public sealed class ModSyncAckHandler : BasePacketHandler<ModSyncPacket>
{
    protected override bool Handle(ModSyncPacket packet, IPEndPoint? clientEp)
    {
        var diff = new List<string>();

        var serverMods = Mods.ToList().ConvertAll(mod => mod.Hash());

        foreach (var clientMod in packet.Mods)
        {
            if (!serverMods.Contains(clientMod.Key))
                diff.Add(clientMod.Value);
        }

        foreach (var serverMod in Mods)
        {
            if (!packet.Mods.ContainsKey(serverMod.Hash()))
                diff.Add(serverMod);
        }

        if (diff.Count == 0) return true;

        SrLogger.LogMessage($"Mods desynchronized!\n\tDifferences: {string.Join(", ", diff)}\n\tClient: {string.Join(", ", packet.Mods.Values)}\n\tServer: {string.Join(", ", Mods)}");

        var reason =
            $"You have incompatible mods!\nMods that are missing or on the wrong version:\n {string.Join("\n", diff)}";
        var denyPacket = new ConnectionDenyPacket { Reason = reason };
        Main.Server.SendToClient(denyPacket, clientEp!);

        Main.Server.clientManager.RemoveClient(clientEp!);

        return false;
    }
}