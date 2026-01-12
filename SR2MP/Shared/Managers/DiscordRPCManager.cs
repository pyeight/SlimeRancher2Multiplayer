using System.Collections.ObjectModel;
using DiscordRPC;
using Il2CppMonomiPark.SlimeRancher.World;

namespace SR2MP.Shared.Managers;

public static class DiscordRPCManager
{
    public enum Zone
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
        
        // Introduce at like, 0.4 or 1.0..?
        FinalBoss,
        Ending,
    }
    // This can be public, do not freak out :)
    public const string DISCORD_APP_ID = "1422276739026911262";
    public static DiscordRpcClient rpcClient;

    public static readonly ReadOnlyDictionary<Zone, string> ZoneToStatus =
        new(new Dictionary<Zone, string>()
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
            {Zone.Ending, "Relaxing after the end"},
        });
    public static readonly ReadOnlyDictionary<string, Zone> DefinitionToZone =
        new(new Dictionary<string, Zone>()
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
            {"Conservatory Pools", Zone.Conservatory},
        });
    public static readonly ReadOnlyDictionary<Zone, string> ZoneToIcon =
        new(new Dictionary<Zone, string>()
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
            {Zone.Ending, "ending"},
        });

    public const string DetailsStringOnline = "Playing in a group of {0} players";
    public const string DetailsStringOnlineSolo = "Playing online, waiting for others";
    public const string DetailsStringOffline = "Playing offline";

    public static void Initialize()
    {
        rpcClient = new DiscordRpcClient(DISCORD_APP_ID);

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
        bool online = Main.Server.IsRunning() || Main.Client.IsConnected;
        bool solo = playerManager.PlayerCount < 2;
        
        string details = online
            ? solo
                ? DetailsStringOnlineSolo
                : string.Format(DetailsStringOnline, playerManager.PlayerCount)
            : DetailsStringOffline;
        var currentLocation = currentZone ? DefinitionToZone[currentZone!.name] : Zone.MainMenu;

        //if (IsInEndingCutscene)
        //    currentLocation = Zone.Ending;
        
        var status = ZoneToStatus[currentLocation];
        var icon = ZoneToIcon[currentLocation];

        rpcClient.SetPresence(new RichPresence
        {
            Details = details,
            State = status,
            Assets = new Assets
            {
                LargeImageKey = icon,
                LargeImageText = string.Empty,
            },
            Buttons = new[]
            {
                new Button
                {
                    Label = "SR2MP Discord",
                    Url = "https://discord.gg/a7wfBw5feU"
                }
            }
        });
    }
}