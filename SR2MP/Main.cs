using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using SR2E.Expansion;
using SR2E.Utils;
using SR2MP.Components;
using SR2MP.Components.FX;
using SR2MP.Components.Player;
using SR2MP.Components.Time;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP;

public sealed class Main : SR2EExpansionV3
{
    public static void SendToAllOrServer(IPacket packet)
    {
        if (Client.IsConnected)
        {
            Client.SendPacket(packet);
        }

        if (Server.IsRunning())
        {
            Server.SendToAll(packet);
        }
    }
    
    public static Client.Client Client { get; private set; }
    public static Server.Server Server { get; private set; }
    static MelonPreferences_Category preferences;
    public static string Username => preferences.GetEntry<string>("username").Value;
    public static bool PacketSizeLogging => preferences.GetEntry<bool>("packet_size_log").Value;

    public override void OnLateInitializeMelon()
    {
        preferences = MelonPreferences.CreateCategory("SR2MP");
        preferences.CreateEntry("username", "Player");
        preferences.CreateEntry("packet_size_log", false);

        Client = new Client.Client();
        Server = new Server.Server();
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
    }

    public override void AfterGameContext(GameContext gameContext)
    {
        actorManager.Initialize(gameContext);
    }
}