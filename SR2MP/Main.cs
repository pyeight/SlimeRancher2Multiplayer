using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
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
    public override void OnInitializeMelon()
    {
        Main.LoadRPCAssembly();
    }

    [DllImport("kernel32", CharSet = CharSet.Ansi)]
    private static extern IntPtr LoadLibrary(string lpFileName);

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
    public static string SavedConnectPort => preferences.GetEntry<string>("recent_port").Value;
    public static string SavedConnectIP => preferences.GetEntry<string>("recent_ip").Value;
    public static string SavedHostPort => preferences.GetEntry<string>("host_port").Value;
    internal static bool SetupUI => preferences.GetEntry<bool>("internal_setup_ui").Value;
    public static bool PacketSizeLogging => preferences.GetEntry<bool>("packet_size_log").Value;
    public static bool AllowCheats => preferences.GetEntry<bool>("allow_cheats").Value;

    // Made this because of a bug in the server handler of ActorSpawnPacket where TrySpawnNetworkActor
    // was given `packet.Type` instead of `packet.ActorType` causing it to always be RockPlort (persistent id 25)
    public static bool RockPlortBug => preferences.GetEntry<bool>("the_rock_plorts_are_coming").Value;

    public override void OnLateInitializeMelon()
    {
        InsertLicensesFile();
        
        preferences = MelonPreferences.CreateCategory("SR2MP");
        preferences.CreateEntry("username", "Player", is_hidden: true);
        preferences.CreateEntry("allow_cheats", false, is_hidden: true);

        preferences.CreateEntry("recent_port", "", is_hidden: true);
        preferences.CreateEntry("recent_ip", "127.0.0.1", is_hidden: true);
        preferences.CreateEntry("host_port", "1919", is_hidden: true);

        preferences.CreateEntry("packet_size_log", false, display_name: "Packet Size Logging");

        preferences.CreateEntry("internal_setup_ui", true, is_hidden: true);

        preferences.CreateEntry("the_rock_plorts_are_coming", false,
            display_name: "<color=#ff0000>The rock plorts are coming</color> <alpha=#66>(Rock Plort Mode)");

        Client = new Client.Client();
        Server = new Server.Server();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        switch (sceneName)
        {
            case "SystemCore":
                MainThreadDispatcher.Initialize();
                DiscordRPCManager.Initialize();

                var forceTimeScale = new GameObject("SR2MP_TimeScale").AddComponent<ForceTimeScale>();
                Object.DontDestroyOnLoad(forceTimeScale.gameObject);

                var ui = new GameObject("SR2MP_UI").AddComponent<MultiplayerUI>();
                Object.DontDestroyOnLoad(ui.gameObject);

                Server.OnServerStarted += () => CheatsEnabled = AllowCheats;

                Application.quitting += new System.Action((() =>
                {
                    if (Server.IsRunning())
                        Server.Close();
                    if (Client.IsConnected)
                        Client.Disconnect();
                }));

                playerManager.OnPlayerAdded += id => DiscordRPCManager.UpdatePresence();

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
        var pref = preferences.GetEntry<T>(key);
        pref.Value = value;
        MelonPreferences.Save();
    }

    internal static void LoadRPCAssembly()
    {
        Stream manifestResourceStream = Assembly.GetCallingAssembly().GetManifestResourceStream("SR2MP.DiscordRPC.dll")!;
        byte[] array = new byte[manifestResourceStream.Length];
        _ = manifestResourceStream.Read(array, 0, array.Length);
        Assembly.Load(array);
    }
    internal static void InsertLicensesFile()
    {
        Stream manifestResourceStream = Assembly.GetCallingAssembly().GetManifestResourceStream("SR2MP.THIRD-PARTY-NOTICES.txt")!;
        byte[] array = new byte[manifestResourceStream.Length];
        _ = manifestResourceStream.Read(array, 0, array.Length);
        File.WriteAllBytes(MelonEnvironment.UserDataDirectory + "/SR2MP/THIRD-PARTY-NOTICES.txt", array);
    }
}