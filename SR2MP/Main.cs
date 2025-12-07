using MelonLoader;
using SR2E;
using SR2E.Expansion;
using SR2E.Utils;
using Main = SR2MP.Main;

namespace SR2MP;

public static class BuildInfo                                                                                           // Adds the Info to the SR2E Mod Menu.
{
    public const string Name = "Slime Rancher 2 Multiplayer Mod";                                                       // Name of the Expansion. 
    public const string Description = "Adds Multiplayer to Slime Rancher 2";                                            // Description for the Expansion.
    public const string Author = "Shark";                                                                               // Author of the Expansion.
    public const string CoAuthors = null;                                                                               // CoAuthor(s) of the Expansion.  (optional, set as null if none)
    public const string Contributors = "Gopher, Artur";                                                                 // Contributor(s) of the Expansion.  (optional, set as null if none)
    public const string Company = null;                                                                                 // Company that made the Expansion.  (optional, set as null if none)
    public const string Version = "1.0.0";                                                                              // Version of the Expansion.
    public const string DownloadLink = null;                                                                            // Download Link for the Expansion.  (optional, set as null if none)
    public const string SourceCode = "https://github.com/pyeight/SlimeRancher2Multiplayer";                             // Source Link for the Expansion.  (optional, set as null if none)
    public const string Nexus = null;                                                                                   // Nexus Link for the Expansion.  (optional, set as null if none)
    public const bool UsePrism = false;                                                                                 // Enable if you use Prism
}

public class HostCommand : SR2ECommand  // We should seperate the commands from this file later - if possible
{
    public override bool Execute(string[] args)
    {
        // MultiplayerManager.Instance.Host(); <- Tarr's code
        return true;
    }

    public override string ID => "host";
    public override string Usage => "host <port>";
}
public class JoinCommand : SR2ECommand
{
    public override bool Execute(string[] args)
    {
        // MultiplayerManager.Instance.Connect(args[0]); <- Tarr's code
        return true;
    }

    public override string ID => "join";
    public override string Usage => "join <code>";
}

public class Main : SR2EExpansionV2
{
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        SR2MP.Logger.Log($"test log owo :3 - {sceneName}");
    }
}
