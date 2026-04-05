// ReSharper disable once CheckNamespace
namespace JetBrains.Annotations;

[AttributeUsage(AttributeTargets.All)]
internal sealed class UsedImplicitlyAttribute : Attribute { }

[Flags]
internal enum ImplicitUseTargetFlags : byte
{
    // ReSharper disable once UnusedMember.Global
    Default = 0,
    Itself = 1,
    Members = 2,
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
internal sealed class PublicAPIAttribute : Attribute { }