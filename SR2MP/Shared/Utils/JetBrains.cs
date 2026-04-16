// ReSharper disable once CheckNamespace
namespace JetBrains.Annotations;

[AttributeUsage(AttributeTargets.All)]
internal sealed class UsedImplicitlyAttribute : Attribute { }

[Flags]
#pragma warning disable S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
internal enum ImplicitUseTargetFlags : byte
#pragma warning restore S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
{
    // ReSharper disable once UnusedMember.Global
#pragma warning disable S2346 // Flags enumerations zero-value members should be named "None"
    Default = 0,
#pragma warning restore S2346 // Flags enumerations zero-value members should be named "None"
    Itself = 1 << 0,
    Members = 1 << 1,
    WithMembers = Itself | Members
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class MeansImplicitUseAttribute : Attribute
{
    public MeansImplicitUseAttribute() { }

    // ReSharper disable once UnusedParameter.Local
#pragma warning disable RCS1163 // Unused parameter
    public MeansImplicitUseAttribute(ImplicitUseTargetFlags targetFlags) { }
#pragma warning restore RCS1163 // Unused parameter
}

[AttributeUsage(AttributeTargets.All)]
internal sealed class PublicApiAttribute : Attribute { }