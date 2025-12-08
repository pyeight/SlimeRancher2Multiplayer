using System.Reflection;
using MelonLoader;
using SR2E.Expansion;

[assembly: AssemblyTitle(Main.BuildInfo.Name)]
[assembly: AssemblyDescription(Main.BuildInfo.Description)]
[assembly: AssemblyVersion(Main.BuildInfo.Version)]
[assembly: AssemblyFileVersion(Main.BuildInfo.Version)]
[assembly: AssemblyCompany(Main.BuildInfo.Company)]
[assembly: AssemblyProduct(Main.BuildInfo.Name)]
[assembly: AssemblyCopyright($"Created by {Main.BuildInfo.Author}")]
[assembly: AssemblyTrademark(Main.BuildInfo.Company)]

[assembly: MelonInfo(typeof(Main), Main.BuildInfo.Name, Main.BuildInfo.Version, Main.BuildInfo.Author, Main.BuildInfo.DownloadLink)]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]
[assembly: MelonColor(255, 77, 149, 203)]
[assembly: MelonAdditionalDependencies("SR2E")]
[assembly: MelonPriority(-100)]
[assembly: VerifyLoaderVersion(0, 6, 2, true)]

[assembly: AssemblyMetadata("co_authors", Main.BuildInfo.CoAuthors)]
[assembly: AssemblyMetadata("contributors", Main.BuildInfo.Contributors)]
[assembly: AssemblyMetadata("source_code", Main.BuildInfo.SourceCode)]
[assembly: AssemblyMetadata("nexus", Main.BuildInfo.Nexus)]
[assembly: AssemblyMetadata("discord", Main.BuildInfo.Discord)]

[assembly: SR2EExpansion(Main.BuildInfo.UsePrism)]