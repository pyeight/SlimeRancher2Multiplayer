internal static class BuildInfo
{
    internal const string Name = "Slime Rancher 2 Multiplayer Mod";
    internal const string Description = "Adds Multiplayer to Slime Rancher 2";
    internal const string Author = "Shark";
    internal const string CoAuthors = "";
    internal const string Contributors = "AlchlcSystm, shizophrenicgopher, PinkTarr";
    internal const string Company = "";
    //Version is shown my ML
    //DisplayVersion is shown by SR2E
    //DisplayVersion automatically gets a -dev at the end if SR2MP is compiled by GitHub Action
    internal const string Version = "0.2.1";
    internal const string DisplayVersion = "0.2.1"; // Auto-Dev_Do_not_remove
    internal const string DownloadLink = "https://discord.com/invite/a7wfBw5feU";
    internal const string SourceCode = "https://github.com/pyeight/SlimeRancher2Multiplayer";
    internal const string Nexus = "";
    internal const string UsePrism = "false";
    internal const string Discord = null; // Discord Link for th Expansion.  (optional, set as null if none)
    internal const string MinSr2EVersion = SR2E.BuildInfo.CodeVersion; // e.g "3.4.3", the min required SR2 version. No beta or alpha versions
    internal const string RequiredGameVersion = "1.1.0"; //e.g 1.1.0 or something similar (optional)
    internal const string ExactRequiredGameVersion = "1.1.0 [20251101030000-104680]"; //e.g 1.1.0 or something similar (optional)
}