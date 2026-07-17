using System.Reflection;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using Starlight.Expansion;
using SR2MP.Client;
using SR2MP.Components.FX;
using SR2MP.Components.Player;
using SR2MP.Components.Time;
using SR2MP.Components.UI;
using SR2MP.Packets.Utils;
using SR2MP.Server;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;
using Starlight.Enums;
using Starlight.Storage;
using UnityEngine.UI;

namespace SR2MP;

/// <summary>
/// The main expansion class serving as the core entry point and configuration hub for the multiplayer environment.
/// </summary>
[StarlightLoadExpansion]
public sealed class Main : StarlightExpansionV01
{
    protected override StarlightPackageInfo info => new ()
    {
        ID = BuildInfo.ID,
        Name = BuildInfo.Name,
        Author = BuildInfo.Author,
        CoAuthors = BuildInfo.CoAuthors,
        Contributors = BuildInfo.Contributors,
        Description = BuildInfo.Description,
        SourceCode = BuildInfo.SourceCode,
        Version = BuildInfo.Version,
        Nexus = BuildInfo.Nexus,
        Discord = BuildInfo.Discord,
        UsePrism = BuildInfo.UsePrism,
        IconPath = "Assets/PlayerMarker.png",
        
        LoadTime = ExpansionLoadTime.Startup,
        UnloadTime = ExpansionUnloadTime.Never,
        MultiplayerRequirement = MultiplayerRequirement.ServerAndClient,
    };

    /// <summary>
    /// Randomization aspect of the mod
    /// Pass in a seed that can be the same between clients before attempting to randomize.
    /// </summary>
    public static Randoms ModRandomization { get; } = new();

    /// <summary>
    /// Gets the active multiplayer client instance.
    /// </summary>
    public static SR2MPClient Client { get; private set; }

    /// <summary>
    /// Gets the active multiplayer server instance.
    /// </summary>
    public static SR2MPServer Server { get; private set; }

    internal static readonly Assembly Core = typeof(Main).Assembly;

    private static MelonPreferences_Category preferences;

    /// <summary>
    /// Gets the local player's username string from preferences.
    /// </summary>
    public static string RawUsername => preferences.GetEntry<string>("username").Value;

    /// <summary>
    /// Gets the local player's username color as hex.
    /// </summary>
    public static string UsernameColor => preferences.GetEntry<string>("username_color").Value;

    /// <summary>
    /// Gets the local player's display name with color
    /// </summary>
    public static string Username
    {
        get
        {
            var name = RawUsername;
            var color = UsernameColor;

            return Extensions.IsValidHexColor(color) ? $"<color=#{color}>{name}</color>" : name;
        }
    }

    /// <summary>
    /// Gets a value indicating whether cheats are allowed based on the user's local configuration.
    /// </summary>
    public static bool AllowCheats => preferences.GetEntry<bool>("allow_cheats").Value;

    /// <summary>
    /// Gets a value indicating whether streamer mode is enabled, which typically hides sensitive information like IP addresses.
    /// </summary>
    public static bool StreamerMode => preferences.GetEntry<bool>("streamer_mode").Value;

    internal static string SavedConnectPort => preferences.GetEntry<string>("recent_port").Value;
    internal static string SavedConnectIP => preferences.GetEntry<string>("recent_ip").Value;
    internal static string SavedHostPort => preferences.GetEntry<string>("host_port").Value;
    internal static bool SetupUI => preferences.GetEntry<bool>("internal_setup_ui_new").Value;
    internal static bool PacketSizeLogging => preferences.GetEntry<bool>("packet_size_log").Value;
    internal static bool PacketAcknowledgeLogging => preferences.GetEntry<bool>("packet_ack_log").Value;
    internal static bool RemoteGadgetPreviewShaders => preferences.GetEntry<bool>("remote_gadget_preview_shaders").Value;

    // Made this because of a bug in the server handler of ActorSpawnPacket where TrySpawnNetworkActor
    // was given `packet.Type` instead of `packet.ActorType` causing it to always be RockPlort (persistent id 25)
    internal static bool RockPlortBug => preferences.GetEntry<bool>("the_rock_plorts_are_coming").Value;

    /// <inheritdoc/>
    public override void OnLateInitializeMelon()
    {
        preferences = MelonPreferences.CreateCategory("SR2MP");
        preferences.CreateEntry("username", "Player", is_hidden: true);
        preferences.CreateEntry("username_color", "FFFFFF", is_hidden: true);
        preferences.CreateEntry("allow_cheats", false, is_hidden: true);
        preferences.CreateEntry("streamer_mode", false, display_name: "Streamer Mode");

        preferences.CreateEntry("recent_port", string.Empty, is_hidden: true);
        preferences.CreateEntry("recent_ip", string.Empty, is_hidden: true);
        preferences.CreateEntry("host_port", "1919", is_hidden: true);
        preferences.CreateEntry("firewall_exceptions", string.Empty, is_hidden: true);

        Firewall.Initialize(preferences.GetEntry<string>("firewall_exceptions"));

        preferences.CreateEntry("packet_size_log", false, display_name: "Packet Size Logging");
        preferences.CreateEntry("packet_ack_log", true, display_name: "Packet Acknowledge Logging");
        preferences.CreateEntry("remote_gadget_preview_shaders", false, display_name: "Remote Gadget Preview Hologram Shader");

        preferences.CreateEntry("internal_setup_ui_new", true, is_hidden: true);

        preferences.CreateEntry("the_rock_plorts_are_coming", false,
            display_name: "<color=#ff0000>The rock plorts are coming</color> <alpha=#66>(Rock Plort Mode), BREAKS SAVES!");

        InsertLicenseFiles();

        Client = new SR2MPClient();
        Server = new SR2MPServer();
    }

    /// <inheritdoc/>
    public override void OnInitialize() => LoadBundledAssemblies();

    internal static void SendToAllOrServer<T>(T packet) where T : IPacket
    {
        if (Client.IsConnected)
            Client.SendPacket(packet);

        if (Server.IsRunning)
            Server.SendToAll(packet);
    }

    /// <inheritdoc/>
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        switch (sceneName)
        {
            case "SystemCore":
                StartupCheck.Initialize();
                MainThreadDispatcher.Initialize();
                DiscordRPCManager.Initialize();

                var forceTimeScale = new GameObject("SR2MP_TimeScale").AddComponent<ForceTimeScale>();
                Object.DontDestroyOnLoad(forceTimeScale.gameObject);

                var ui = new GameObject("SR2MP_UI").AddComponent<MultiplayerUI>();
                Object.DontDestroyOnLoad(ui.gameObject);

                Server.OnServerStarted += () => CheatsEnabled = AllowCheats;

                Application.quitting += new Action(() =>
                {
                    DiscordRPCManager.Shutdown();
                    if (Server.IsRunning)
                        Server.Close();
                    if (Client.IsConnected)
                        Client.Disconnect();
                });

                PlayerManager.OnPlayerAdded += _ => DiscordRPCManager.UpdatePresence();

                break;

            case "MainMenuEnvironment":
                InitializePlayer("BeatrixMainMenu", 0.85f);
                break;
        }
    }

    /// <inheritdoc/>
    public override void AfterGameContext(GameContext gameContext)
    {
        ActorManager.Initialize(gameContext);
        NetworkSceneManager.Initialize(gameContext);
        NetworkAmmoManager.Initialize();
        NetworkDroneManager.Initialize();
    }

    internal static void SetConfigValue<T>(string key, T value)
    {
        preferences.GetEntry<T>(key).Value = value;
        MelonPreferences.Save();
    }

    private static void LoadBundledAssemblies()
    {
        LoadBundledAssemblyResource("DiscordRPC");
        LoadBundledAssemblyResource("SharpOpenNat");
    }

    private static void LoadBundledAssemblyResource(string resourceName)
    {
        var fileName = $"{resourceName}.dll";

        try
        {
            if (AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == resourceName))
            {
                SrLogger.LogMessage($"Dependency {fileName} is already loaded.");
                return;
            }

            var bytes = EmbeddedResourceEUtil.LoadResource($"Bundled.{fileName}");

            if (bytes == null)
            {
                SrLogger.LogWarning("Missing embedded dependency: " + fileName);
                return;
            }

            Assembly.Load(bytes);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to load {fileName}: {ex.Message}");
        }
    }

    private static void InsertLicenseFiles()
    {
        InsertLicenseFile("DiscordRPC");
        InsertLicenseFile("SharpOpenNat");
    }

    private static void InsertLicenseFile(string resourceName)
    {
        var dirPath = Path.Combine(MelonEnvironment.UserDataDirectory, "SR2MP");
        var filePath = Path.Combine(dirPath, resourceName + ".lic.txt");

        if (File.Exists(filePath))
            return;

        try
        {
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            var bytes = EmbeddedResourceEUtil.LoadResource($"Bundled.{resourceName}.lic.txt");

            if (bytes == null)
            {
                SrLogger.LogWarning("Missing embedded dependency: " + resourceName + ".lic.txt");
                return;
            }

            File.WriteAllBytes(filePath,bytes);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to extract license for {resourceName}: {ex}");
        }
    }

    private static void InitializePlayer(string objName, float scale)
    {
        PlayerPrefab = new GameObject("PLAYER");
        PlayerPrefab.SetActive(false);
        PlayerPrefab.transform.localScale = Vector3.one * scale;

        var audio = PlayerPrefab.AddComponent<SECTR_PointSource>();
        audio.instance = new SECTR_AudioCueInstance();

        var networkComponent = PlayerPrefab.AddComponent<NetworkPlayer>();

        var playerModel = Object.Instantiate(GameObject.Find(objName)).transform;
        playerModel.parent = PlayerPrefab.transform;
        playerModel.localPosition = Vector3.zero;
        playerModel.localRotation = Quaternion.identity;
        playerModel.localScale = Vector3.one;

        var name = new GameObject("Username")
        {
            transform = { parent = PlayerPrefab.transform, localPosition = Vector3.up * 3 }
        };

        var textComponent = name.AddComponent<TextMeshPro>();

        networkComponent.UsernamePanel = textComponent;

        var footstepFX = new GameObject("Footstep") { transform = { parent = PlayerPrefab.transform } };
        PlayerPrefab.AddComponent<NetworkPlayerFootstep>().SpawnAtTransform = footstepFX.transform;

        Object.DontDestroyOnLoad(PlayerPrefab);
        
        PlayerCompassPrefab = new GameObject("PlayerCompassMarker");
        PlayerCompassPrefab.SetActive(false);
        PlayerCompassPrefab.transform.localPosition = Vector3.zero;
        PlayerCompassPrefab.transform.localRotation = Quaternion.identity;
        PlayerCompassPrefab.transform.localScale = Vector3.one;
        PlayerCompassPrefab.AddComponent<Image>().sprite = EmbeddedResourceEUtil.LoadSprite("Assets.PlayerMarker.png").CopyWithoutMipmaps();
        var rectTransform = PlayerCompassPrefab.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.0f);
        rectTransform.sizeDelta = new Vector2Int(32, 32);
        PlayerMapPrefab = Object.Instantiate(PlayerCompassPrefab);
        PlayerMapPrefab.name = "PlayerMapMarker";
        var label = new GameObject("NameLabel")
        {
            transform =
            {
                parent = PlayerCompassPrefab.transform,
                localPosition = Vector3.down * 40f,
                localScale = Vector3.one * 0.585f
            }
        };
        var gui = label.AddComponent<TextMeshProUGUI>();
        gui.alignment = TextAlignmentOptions.Center;
        gui.font = GetFont("Runsell Type - HemispheresCaps2 (Latin)");
        
        Object.DontDestroyOnLoad(PlayerCompassPrefab);
    }
    private static TMP_FontAsset GetFont(string fontName) => Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(x => x.name == fontName)!;
}