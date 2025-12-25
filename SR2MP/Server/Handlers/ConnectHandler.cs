using System.Collections;
using System.Net;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using MelonLoader;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Server.Models;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.Connect)]
public class ConnectHandler : BasePacketHandler
{
    public ConnectHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
    {
        using var reader = new PacketReader(data);
        reader.Skip(1);

        var playerId = reader.ReadString();

        SrLogger.LogMessage($"Connect request received with PlayerId: {playerId}",
            $"Connect request from {senderEndPoint} with PlayerId: {playerId}");

        var client = clientManager.AddClient(senderEndPoint, playerId);

        var ackPacket = new ConnectAckPacket
        {
            Type = (byte)PacketType.ConnectAck,
            PlayerId = playerId,
            OtherPlayers = Array.ConvertAll(playerManager.GetAllPlayers().ToArray(), input => input.PlayerId)
        };

        Main.Server.SendToClient(ackPacket, client);

        var joinPacket = new PlayerJoinPacket
        {
            Type = (byte)PacketType.PlayerJoin, PlayerId = playerId, PlayerName = Main.Username
        };

        Main.Server.SendToAllExcept(joinPacket, senderEndPoint);
        
        SendActorsPacket(senderEndPoint);
        SendPlotsPacket(senderEndPoint);
        
        SrLogger.LogMessage($"Player {playerId} successfully connected",
            $"Player {playerId} successfully connected from {senderEndPoint}");
    }

    void SendActorsPacket(IPEndPoint client)
    {
        var actorsList = new List<ActorsPacket.Actor>();
        
        foreach (var actorKeyValuePair in SceneContext.Instance.GameModel.identifiables)
        {
            var actor = actorKeyValuePair.Value;
            var model = actor.TryCast<ActorModel>();
            var rotation = model?.lastRotation ?? Quaternion.identity;
            actorsList.Add(new ActorsPacket.Actor()
            {
                ActorId = actor.actorId.Value,
                ActorType = actorManager.GetPersistentID(actor.ident),
                Position = actor.lastPosition,
                Rotation = rotation,
            });
        }

        var actorsPacket = new ActorsPacket() 
        { 
            Type = (byte)PacketType.InitialActors,
            Actors = actorsList
        };

        Main.Server.SendToClient(actorsPacket, client);
    }
    void SendPlotsPacket(IPEndPoint client)
    {
        var plotsList = new List<LandPlotsPacket.Plot>();
        
        foreach (var plotKeyValuePair in SceneContext.Instance.GameModel.landPlots)
        {
            var plot = plotKeyValuePair.Value;
            var id = plotKeyValuePair.Key;
            
            plotsList.Add(new LandPlotsPacket.Plot()
            {
                ID = id,
                Type = plot.typeId,
                UpgradesList = plot.upgrades.Cast<Il2CppSystem.Collections.Generic.List<LandPlot.Upgrade>>(),
            });
        }

        var plotsPacket = new LandPlotsPacket() 
        { 
            Type = (byte)PacketType.InitialPlots,
            Plots = plotsList
        };

        Main.Server.SendToClient(plotsPacket, client);
    }
}