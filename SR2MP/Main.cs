using Il2CppTMPro;
using MelonLoader;
using SR2E.Expansion;
using SR2MP.Components.FX;
using SR2MP.Components.Player;
using SR2MP.Components.Time;
using SR2MP.Components.UI;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP;

public sealed class Main : SR2EExpansionV3
{
    public static void SendToAllOrServer<T>(T packet) where T : IPacket
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
    internal static bool SetupUI => preferences.GetEntry<bool>("internal_setup_ui").Value;
    public static bool PacketSizeLogging => preferences.GetEntry<bool>("packet_size_log").Value;
    public static bool AllowCheats => preferences.GetEntry<bool>("allow_cheats").Value;

    public override void OnLateInitializeMelon()
    {
        preferences = MelonPreferences.CreateCategory("SR2MP");
        preferences.CreateEntry("username", "Player").IsHidden = true;
        preferences.CreateEntry("packet_size_log", false);
        preferences.CreateEntry("internal_setup_ui", true).IsHidden = true;
        preferences.CreateEntry("allow_cheats", false).IsHidden = true;

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

                var ui = new GameObject("SR2MP_UI").AddComponent<MultiplayerUI>();
                Object.DontDestroyOnLoad(ui.gameObject);

                Server.OnServerStarted += () => CheatsEnabled = AllowCheats;

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
        NetworkSceneManager.Initialize(gameContext);

        // Automatically inserts just by running the constructor.
        //new CustomPauseMenuButton(
        //    SR2ELanguageManger.AddTranslation("Multiplayer", "b.multiplayer", "UI"),
        //    5,
        //    () => SrLogger.LogMessage("Multiplayer menu open"));

    }

    internal static void SetConfigValue<T>(string key, T value)
    {
        preferences.GetEntry<T>(key).Value = value;
    }
}