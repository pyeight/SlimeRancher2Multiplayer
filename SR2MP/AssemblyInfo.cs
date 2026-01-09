using System.Reflection;
using MelonLoader;
using SR2E.Expansion;

// PLEASE COPY THIS FILE INTO YOUR PROJECT AS IS!
// I WILL NOT - Az

// Leave this as is
[assembly: AssemblyTitle(BuildInfo.Name)]
[assembly: AssemblyDescription(BuildInfo.Description)]
[assembly: AssemblyCompany(BuildInfo.Company)]
[assembly: AssemblyProduct(BuildInfo.Name)]
[assembly: AssemblyCopyright($"Created by {BuildInfo.Author}")]
[assembly: AssemblyTrademark(BuildInfo.Company)]
[assembly: AssemblyVersion(BuildInfo.Version)]
[assembly: AssemblyFileVersion(BuildInfo.Version)]

[assembly: MelonInfo(typeof(MLEntrypoint), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author, BuildInfo.DownloadLink)]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

[assembly: AssemblyMetadata(SR2EExpansionAttributes.CoAuthors, BuildInfo.CoAuthors)]
[assembly: AssemblyMetadata(SR2EExpansionAttributes.MinSR2EVersion, BuildInfo.MinSr2EVersion)]
[assembly: AssemblyMetadata(SR2EExpansionAttributes.Contributors, BuildInfo.Contributors)]
[assembly: AssemblyMetadata(SR2EExpansionAttributes.SourceCode, BuildInfo.SourceCode)]
[assembly: AssemblyMetadata(SR2EExpansionAttributes.Nexus, BuildInfo.Nexus)]
[assembly: AssemblyMetadata(SR2EExpansionAttributes.UsePrism, BuildInfo.UsePrism)]
[assembly: AssemblyMetadata(SR2EExpansionAttributes.IsExpansion, "true")]

[assembly: MelonAdditionalDependencies("SR2E")]

// Modifies the minimum ML version required (mandatory)
[assembly: VerifyLoaderVersion(0, 7, 1, true)]
// Sets a color of your melon (mandatory)
[assembly: MelonColor(255, 77, 149, 203)]

#pragma warning disable RCS1110 // Declare type inside namespace

//Set your main class inside the typeof argument, it has to be an SR2EExpansion
internal static class GetEntrypointType
{
    // ReSharper disable once InconsistentNaming
    public static Type type => typeof(Main);
}

// BuildInfo
internal static class BuildInfo
{
    internal const string Name = "Slime Rancher 2 Multiplayer Mod";
    internal const string Description = "Adds Multiplayer to Slime Rancher 2";
    internal const string Author = "Shark";
    internal const string CoAuthors = "";
    internal const string Contributors = "AlchlcSystm, shizophrenicgopher, PinkTarr";
    internal const string Company = "";
    internal const string Version = "0.1.3";
    internal const string DownloadLink = "https://discord.com/invite/a7wfBw5feU";
    internal const string SourceCode = "https://github.com/pyeight/SlimeRancher2Multiplayer";
    internal const string Nexus = "";
    internal const string UsePrism = "false";
    internal const string MinSr2EVersion = SR2E.BuildInfo.CodeVersion; // e.g "3.4.3", the min required SR2 version. No beta or alpha versions
}