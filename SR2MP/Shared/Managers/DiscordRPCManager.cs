using System.Collections.ObjectModel;
using DiscordRPC;
using Il2CppMonomiPark.SlimeRancher.World;

namespace SR2MP.Shared.Managers;

public static class DiscordRPCManager
{
    private enum Zone : byte
    {
        Conservatory,
        RainbowFields,
        StarlightStand,
        EmberValley,
        PowderfallBluffs,
        LabyrinthWaterworks,
        LabyrinthLavaDepths,
        LabyrinthDreamland,
        LabyrinthHub,
        LabyrinthTerrarium,
        LabyrinthCore,
        MainMenu,

        // Introduce at like, 0.4 or 1.0.?
        FinalBoss,
        Ending
    }
    // This can be public, do not freak out :)
    private const string DiscordAppID = "1422276739026911262";
    private static DiscordRpcClient? rpcClient;

    private static readonly ReadOnlyDictionary<Zone, string> ZoneToStatus =
        new(new Dictionary<Zone, string>
        {
            {Zone.Conservatory, "Ranching at the Conservatory"},
            {Zone.RainbowFields, "Exploring the Rainbow Fields"},
            {Zone.StarlightStand, "Amazed by the Starlight Strands"},
            {Zone.EmberValley, "Finding Boom Slimes in the Ember Valley"},
            {Zone.PowderfallBluffs, "Building snowmen in the Powderfall Bluffs"},
            {Zone.LabyrinthTerrarium, "Exploring the mossy depths of the Terrarium"},
            {Zone.LabyrinthLavaDepths, "Heating up in the Lava Depths"},
            {Zone.LabyrinthWaterworks, "Splashing in the Waterworks"},
            {Zone.LabyrinthDreamland, "Sleeping peacefully in the Dreamland"},
            {Zone.LabyrinthHub, "Staring at the Impossible Sky"},
            {Zone.LabyrinthCore, "Inspecting the Core"},
            {Zone.MainMenu, "Getting ready for adventures!"},
            {Zone.FinalBoss, "Fighting it."},
            {Zone.Ending, "Relaxing after the end"}
        });

    private static readonly ReadOnlyDictionary<string, Zone> DefinitionToZone =
        new(new Dictionary<string, Zone>
        {
            {"Conservatory", Zone.Conservatory},
            {"Labyrinth hub", Zone.LabyrinthHub},
            {"RainbowFields", Zone.RainbowFields},
            {"Luminous Strand", Zone.StarlightStand},
            {"Rumbling Gorge", Zone.EmberValley},
            {"Zoo_Debug", Zone.MainMenu},
            {"Powderfall Bluffs", Zone.PowderfallBluffs},
            {"Labyrinth dreamland", Zone.LabyrinthDreamland},
            {"Labyrinth valley entrance", Zone.LabyrinthLavaDepths},
            {"Labyrinth strand entrance", Zone.LabyrinthWaterworks},
            {"Labyrinth terrarium", Zone.LabyrinthTerrarium},
            {"Labyrinth core", Zone.LabyrinthCore},
            {"Conservatory Archway", Zone.Conservatory},
            {"Conservatory Den", Zone.Conservatory},
            {"Conservatory Digsite", Zone.Conservatory},
            {"Conservatory Gully", Zone.Conservatory},
            {"Conservatory Pools", Zone.Conservatory}
        });

    private static readonly ReadOnlyDictionary<Zone, string> ZoneToIcon =
        new(new Dictionary<Zone, string>
        {
            {Zone.Conservatory, "conservatory"},
            {Zone.RainbowFields, "rainbowfields"},
            {Zone.StarlightStand, "starlightstand"},
            {Zone.EmberValley, "embervalley"},
            {Zone.PowderfallBluffs, "powderfallbluffs"},
            {Zone.LabyrinthHub, "impossiblesky"},
            {Zone.LabyrinthTerrarium, "terrarium"},
            {Zone.LabyrinthLavaDepths, "lavadepths"},
            {Zone.LabyrinthWaterworks, "waterworks"},
            {Zone.LabyrinthDreamland, "dreamland"},
            {Zone.LabyrinthCore, "core"},
            {Zone.MainMenu, "mainmenu"},
            {Zone.FinalBoss, "battle"},
            {Zone.Ending, "ending"}
        });

    private const string DetailsStringOnline = "Playing in a group of {0} players";
    private const string DetailsStringOnlineSolo = "Playing online, waiting for others";
    private const string DetailsStringOffline = "Playing offline";

    public static void Initialize()
    {
        rpcClient = new DiscordRpcClient(DiscordAppID);

        rpcClient.Initialize();

        UpdatePresence();
    }

    public static void Shutdown()
    {
        rpcClient?.Dispose();
    }

    public static ZoneDefinition? currentZone;
    public static bool IsInEndingCutscene => SystemContext.Instance.SceneLoader._currentSceneGroup.name == "OutroSequence";

    internal static void UpdatePresence()
    {
        var online = Main.Server.IsRunning() || Main.Client.IsConnected;
        var solo = playerManager.PlayerCount < 2;

        var details = online
            ? solo
                ? DetailsStringOnlineSolo
                : string.Format(DetailsStringOnline, playerManager.PlayerCount)
            : DetailsStringOffline;
        var currentLocation = currentZone ? DefinitionToZone[currentZone!.name] : Zone.MainMenu;

        //if (IsInEndingCutscene)
        //    currentLocation = Zone.Ending;

        var status = ZoneToStatus[currentLocation];
        var icon = ZoneToIcon[currentLocation];

        rpcClient?.SetPresence(new RichPresence
        {
            Details = details,
            State = status,
            Assets = new Assets
            {
                LargeImageKey = icon,
                LargeImageText = string.Empty
            },
            Buttons = new[]
            {
                new Button
                {
                    Label = "SR2 Multiplayer Discord",
                    Url = "https://discord.gg/a7wfBw5feU"
                }
            }
        });
    }
}