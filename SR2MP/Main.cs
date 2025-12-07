using MelonLoader;
using SR2E;
using SR2E.Expansion;
using SR2E.Utils;
using Main = SR2MP.Main;

namespace SR2MP;
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
    public static class BuildInfo
    {
        public const string Name = "Slime Rancher 2 Multiplayer Mod";
        public const string Description = "Adds Multiplayer to Slime Rancher 2";
        public const string Author = "Shark";
        public const string CoAuthors = null;
        public const string Contributors = "Gopher, Artur"; 
        public const string Company = null;
        public const string Version = "0.1.0-a";
        public const string DownloadLink = null;
        public const string SourceCode = "https://github.com/pyeight/SlimeRancher2Multiplayer";
        public const string Nexus = null;
        public const bool UsePrism = false;
    }
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        SR2MP.Logger.Log($"test log owo :3 - {sceneName}");
    }
}
