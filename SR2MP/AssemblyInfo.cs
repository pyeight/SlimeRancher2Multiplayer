using System.Reflection;
using MelonLoader;

[assembly: AssemblyTitle(BuildInfo.Name)]
[assembly: AssemblyDescription(BuildInfo.Description)]
[assembly: AssemblyCompany(BuildInfo.Author)]
[assembly: AssemblyProduct(BuildInfo.Name)]
[assembly: AssemblyCopyright($"Created by {BuildInfo.Author}")]
[assembly: AssemblyTrademark(BuildInfo.Author)]
[assembly: AssemblyVersion(BuildInfo.MelonVersion)]
[assembly: AssemblyFileVersion(BuildInfo.MelonVersion)]


[assembly: MelonOptionalDependencies("DiscordRPC", "SharpOpenNat")]

[assembly: VerifyLoaderVersion(0, 7, 3, true)]
[assembly: MelonColor(255, 77, 149, 203)]
