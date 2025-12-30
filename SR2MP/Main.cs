using Il2CppTMPro;
using MelonLoader;
using SR2E.Expansion;
using SR2MP.Components.FX;
using SR2MP.Components.Player;
using SR2MP.Components.Time;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Server;
using SR2MP.Client;

namespace SR2MP;

public sealed class Main : SR2EExpansionV3
{
    public static string Username = "Player";
    public static bool IsLoadingMultiplayerSave = false;
    public static bool PacketSizeLogging = false;
    public static Server.Server Server { get; private set; }
    public static Client.Client Client { get; private set; }

    public override void OnInitializeMelon()
    {
        Server = new Server.Server();
        Client = new Client.Client();
        SrLogger.LogMessage("SR2MP Initialized");
    }

    public static void SendToAllOrServer<T>(T packet) where T : IPacket
    {
        if (Client != null && Client.IsConnected)
        {
            Client.SendPacket(packet);
        }

        if (Server != null && Server.IsRunning())
        {
            Server.SendToAll(packet);
        }
    }
    
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        switch (sceneName)
        {
            case "SystemCore":
                MainThreadDispatcher.Initialize();

                var forceTimeScale = new GameObject("SR2MP_TimeScale").AddComponent<ForceTimeScale>();
                Object.DontDestroyOnLoad(forceTimeScale.gameObject);
                break;

            case "MainMenuEnvironment":
                playerPrefab = new GameObject("PLAYER");
                playerPrefab.SetActive(false);
                playerPrefab.transform.localScale = Vector3.one * 0.85f;

                var audio = playerPrefab.AddComponent<SECTR_PointSource>();
                audio.instance = new SECTR_AudioCueInstance();

                var networkComponent = playerPrefab.AddComponent<NetworkPlayer>();

                var playerModel = Object.Instantiate(GameObject.Find("BeatrixMainMenu")).transform;
                playerModel.parent = playerPrefab.transform;
                playerModel.localPosition = Vector3.zero;
                playerModel.localRotation = Quaternion.identity;
                playerModel.localScale = Vector3.one;

                var name = new GameObject("Username")
                {
                    transform = { parent = playerPrefab.transform, localPosition = Vector3.up * 3 }
                };

                var textComponent = name.AddComponent<TextMeshPro>();

                networkComponent.usernamePanel = textComponent;

                var footstepFX = new GameObject("Footstep") { transform = { parent = playerPrefab.transform } };
                playerPrefab.AddComponent<NetworkPlayerFootstep>().spawnAtTransform = footstepFX.transform;

                Object.DontDestroyOnLoad(playerPrefab);
                break;
        }

        if (IsLoadingMultiplayerSave && !sceneName.Equals("MainMenuEnvironment") && !sceneName.Equals("SystemCore") && !sceneName.Equals("LoadScene"))
        {
            if (Client != null && Client.PendingJoin != null)
            {
                SrLogger.LogMessage($"Multiplayer Save Loaded in {sceneName}! Finalizing Join...", SrLogger.LogTarget.Both);
                IsLoadingMultiplayerSave = false;

                var pd = Client.PendingJoin;
                
                // Restore Currency
                if(GameContext.Instance.LookupDirector != null && SceneContext.Instance.PlayerState != null)
                {
                    SceneContext.Instance.PlayerState._model.SetCurrency(GameContext.Instance.LookupDirector._currencyList[0].Cast<ICurrency>(), pd.Money);
                    SceneContext.Instance.PlayerState._model.SetCurrency(GameContext.Instance.LookupDirector._currencyList[1].Cast<ICurrency>(), pd.RainbowMoney);
                }

                // Spawn Players
                foreach (var player in pd.OtherPlayers)
                {
                   var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkPlayer>();
                   playerObject.gameObject.SetActive(true);
                   playerObject.ID = player;
                   playerObject.gameObject.name = player;
                   playerObjects[player] = playerObject.gameObject; // Use indexer to add or update
                   playerManager.AddPlayer(player);
                   Object.DontDestroyOnLoad(playerObject);
                }

                // Send Join Packet
                var joinPacket = new PlayerJoinPacket
                {
                    Type = (byte)PacketType.PlayerJoin,
                    PlayerId = pd.PlayerId,
                    PlayerName = Username // Username is from SR2EExpansionV3
                };

                Client.SendPacket(joinPacket);
                Client.StartHeartbeat();
                Client.NotifyConnected();

                Client.PendingJoin = null;
            }
        }
    }

    public override void AfterGameContext(GameContext gameContext)
    {
        actorManager.Initialize(gameContext);
    }
}