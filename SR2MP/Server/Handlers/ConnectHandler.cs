using System.Collections;
using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using MelonLoader;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Server.Models;
using SR2MP.Components.World;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Il2CppMonomiPark.SlimeRancher.World;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.Connect)]
public sealed class ConnectHandler : BasePacketHandler
{
    public ConnectHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, string clientIdentifier)
    {
        using var reader = new PacketReader(data);
        reader.Skip(1);

        var playerId = reader.ReadString();

        SrLogger.LogMessage($"Connect request received with PlayerId: {playerId}",
            $"Connect request from {clientIdentifier} with PlayerId: {playerId}");

        // Parse clientIdentifier (e.g., "[::1]:12345" or "1.2.3.4:5678") back to IPEndPoint
        int lastColon = clientIdentifier.LastIndexOf(':');
        if (lastColon > 0)
        {
            string ipStr = clientIdentifier.Substring(0, lastColon);
            string portStr = clientIdentifier.Substring(lastColon + 1);

            if (ipStr.StartsWith("[") && ipStr.EndsWith("]"))
                ipStr = ipStr.Substring(1, ipStr.Length - 2);

            if (IPAddress.TryParse(ipStr, out var ip) && int.TryParse(portStr, out var port))
            {
                var endPoint = new IPEndPoint(ip, port);
                clientManager.AddClient(clientIdentifier, endPoint, playerId);
            }
        }

        var money = SceneContext.Instance.PlayerState.GetCurrency(GameContext.Instance.LookupDirector._currencyList[0]
            .Cast<ICurrency>());
        var rainbowMoney =
            SceneContext.Instance.PlayerState.GetCurrency(GameContext.Instance.LookupDirector._currencyList[1]
                .Cast<ICurrency>());

        var ackPacket = new ConnectAckPacket
        {
            Type = (byte)PacketType.ConnectAck,
            PlayerId = playerId,
            OtherPlayers = Array.ConvertAll(clientManager.GetAllClients().ToArray(), input => input.PlayerId),
            Money = money,
            RainbowMoney = rainbowMoney
        };

        Main.Server.SendToClient(ackPacket, clientIdentifier);

        var joinPacket = new PlayerJoinPacket
        {
            Type = (byte)PacketType.PlayerJoin, PlayerId = playerId, PlayerName = Main.Username
        };

        Main.Server.SendToAllExcept(joinPacket, clientIdentifier);

        SendPlotsPacket(clientIdentifier);
        SendActorsPacket(clientIdentifier);
        SendGadgetsPacket(clientIdentifier);
        SendUpgradesPacket(clientIdentifier);
        SendPediaPacket(clientIdentifier);

        SrLogger.LogMessage($"Player {playerId} successfully connected",
            $"Player {playerId} successfully connected from {clientIdentifier}");
    }

    void SendUpgradesPacket(string clientIdentifier)
    {
        var upgrades = new Dictionary<byte, sbyte>();

        foreach (var upgrade in GameContext.Instance.LookupDirector._upgradeDefinitions.items)
        {
            upgrades.Add((byte)upgrade._uniqueId, (sbyte)SceneContext.Instance.PlayerState._model.upgradeModel.GetUpgradeLevel(upgrade));
        }

        var upgradesPacket = new UpgradesPacket()
        {
            Type = (byte)PacketType.InitialPlayerUpgrades,
            Upgrades = upgrades,
        };
        Main.Server.SendToClient(upgradesPacket, clientIdentifier);
    } 
    void SendPediaPacket(string clientIdentifier)
    {
        var unlocked = SceneContext.Instance.PediaDirector._pediaModel.unlocked;
        
        var unlockedArray = Il2CppSystem.Linq.Enumerable
            .ToArray(unlocked.Cast<CppCollections.IEnumerable<PediaEntry>>());

        SrLogger.LogMessage($"Found {unlockedArray.Length} pedia entries");
        
        var unlockedIDs = unlockedArray.Select(entry => entry.PersistenceId).ToList();

        SrLogger.LogMessage($"Sent {unlockedIDs.Count} pedia entries");

        var pediasPacket = new PediasPacket()
        {
            Type = (byte)PacketType.InitialPediaEntries,
            Entries = unlockedIDs
        };

        Main.Server.SendToClient(pediasPacket, clientIdentifier);

        SrLogger.LogMessage("InitialPediaEntries packet sent");
    }

    void SendActorsPacket(string clientIdentifier)
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
                ActorType = GlobalVariables.actorManager.GetPersistentID(actor.ident),
                Position = actor.lastPosition,
                Rotation = rotation,
            });
        }

        var actorsPacket = new ActorsPacket()
        {
            Type = (byte)PacketType.InitialActors,
            Actors = actorsList
        };

        Main.Server.SendToClient(actorsPacket, clientIdentifier);
    }

    void SendPlotsPacket(string clientIdentifier)
    {
        var plotsList = new List<LandPlotsPacket.Plot>();
    
        foreach (var plotKeyValuePair in SceneContext.Instance.GameModel.landPlots)
        {
            var plot = plotKeyValuePair.Value;
            var id = plotKeyValuePair.Key;
            
            var upgradesList = new CppCollections.List<LandPlot.Upgrade>();
            foreach (var upgrade in plot.upgrades)
            {
                upgradesList.Add(upgrade);
            }

            plotsList.Add(new LandPlotsPacket.Plot()
            {
                ID = id,
                Type = plot.typeId,
                UpgradesList = upgradesList,
            });
        }

        var plotsPacket = new LandPlotsPacket()
        {
            Type = (byte)PacketType.InitialPlots,
            Plots = plotsList
        };

        Main.Server.SendToClient(plotsPacket, clientIdentifier);
    }

    void SendGadgetsPacket(string clientIdentifier)
    {
        var gadgetsList = new List<GadgetPacket>();
        
        foreach (var entry in GlobalVariables.gadgetsById)
        {
            if (entry.Value == null) continue;
            
            var identifiable = entry.Value.GetComponent<Identifiable>();
            if (identifiable == null) continue;

            var netGadget = entry.Value.GetComponent<NetworkGadget>();
            if (netGadget == null) continue;

            gadgetsList.Add(new GadgetPacket
            {
                Type = (byte)PacketType.Gadget,
                GadgetId = netGadget.GadgetId,
                GadgetTypeId = GlobalVariables.actorManager.GetPersistentID(identifiable.identType),
                Position = entry.Value.transform.position,
                Rotation = entry.Value.transform.rotation,
                IsRemoval = false
            });
        }

        var initialGadgetsPacket = new InitialGadgetsPacket
        {
            Type = (byte)PacketType.InitialGadgets,
            Gadgets = gadgetsList
        };

        Main.Server.SendToClient(initialGadgetsPacket, clientIdentifier);
    }
}
