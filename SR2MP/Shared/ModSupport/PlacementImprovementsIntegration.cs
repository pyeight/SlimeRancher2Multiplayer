using System.Reflection;
using HarmonyLib;
using SR2MP.Packets.Player;
using Starlight.Managers;

namespace SR2MP.Shared.ModSupport;

/// <summary>
/// Integration with the BuildImprovements / PlacementImprovements mod,
/// by Lunar Snail (Discord: lunar_snail / 426024775333314570),
/// which lets players customize the gadget placement and it's colors.
/// </summary>
internal static class PlacementImprovementsIntegration
{
    private const string ModId = "nl.lunarsnail.buildimprovements";

    private static bool initialized;
    private static bool installed;
    private static PropertyInfo? validColorProperty;
    private static PropertyInfo? almostValidColorProperty;
    private static PropertyInfo? invalidColorProperty;
    private static MemberInfo? currentValidityMember;

    /// <summary>
    /// Whether the PlacementImprovements mod is loaded or not.
    /// </summary>
    public static bool IsInstalled
    {
        get
        {
            Initialize();
            return installed;
        }
    }

    /// <summary>
    /// Gets the color BuildImprovements is configured to use for the given placement state.
    /// Returns default when the mod is not installed or the color could not be read.
    /// </summary>
    public static bool TryGetPlacementColor(GadgetPlacementValidity validity, out Color color)
    {
        Initialize();

        var property = validity switch
        {
            GadgetPlacementValidity.Valid => validColorProperty,
            GadgetPlacementValidity.AlmostValid => almostValidColorProperty,
            _ => invalidColorProperty,
        };

        if (property == null)
        {
            color = default;
            return false;
        }

        try
        {
            color = (Color)property.GetValue(null)!;
            return true;
        }
        catch (Exception e)
        {
            validColorProperty = null;
            almostValidColorProperty = null;
            invalidColorProperty = null;
            SrLogger.LogWarning($"BuildImprovements color lookup failed, using vanilla colors: {e.Message}");
            color = default;
            return false;
        }
    }

    /// <summary>
    /// Gets the placement validity BuildImprovements has for the current gadget
    /// Returns default when the mod is not installed or the state could not be read.
    /// </summary>
    public static bool TryGetCurrentValidity(out GadgetPlacementValidity validity)
    {
        Initialize();

        if (currentValidityMember == null)
        {
            validity = default;
            return false;
        }

        try
        {
            var value = currentValidityMember switch
            {
                PropertyInfo property => property.GetValue(null),
                FieldInfo field => field.GetValue(null),
                _ => null,
            };
            validity = (GadgetPlacementValidity)Convert.ToInt32(value);
            return true;
        }
        catch (Exception e)
        {
            currentValidityMember = null;
            SrLogger.LogWarning($"BuildImprovements validity lookup failed: {e.Message}");
            validity = default;
            return false;
        }
    }

    private static void Initialize()
    {
        if (initialized) return;
        initialized = true;

        try
        {
            var packageInfo = StarlightPackageManager.GetPackageInfoFromID(ModId);
            if (packageInfo == null) return;

            installed = true;

            object? entrypoint = packageInfo.Value.GetEntrypoint();
            var mainType = entrypoint?.GetType();
            if (mainType == null) return;

            // The colors are properties on the mod's PreferenceDirector
            // Type scan as fallback in case they move in a future version.
            var colorsType = mainType.Assembly.GetType("BuildImprovements.Preferences.PreferenceDirector") ?? mainType;
            validColorProperty = FindProperty(colorsType, "ValidColor");
            almostValidColorProperty = FindProperty(colorsType, "AlmostValidColor");
            invalidColorProperty = FindProperty(colorsType, "InvalidColor");

            var patchHelperType = mainType.Assembly.GetType("BuildImprovements.Patches.PatchHelper");
            if (patchHelperType != null)
                currentValidityMember = FindValidityMember(patchHelperType);

            if (validColorProperty == null || almostValidColorProperty == null ||
                invalidColorProperty == null || currentValidityMember == null)
            {
                foreach (var type in AccessTools.GetTypesFromAssembly(mainType.Assembly))
                {
                    validColorProperty ??= FindProperty(type, "ValidColor");
                    almostValidColorProperty ??= FindProperty(type, "AlmostValidColor");
                    invalidColorProperty ??= FindProperty(type, "InvalidColor");
                    currentValidityMember ??= FindValidityMember(type);
                }
            }
        }
        catch (Exception e)
        {
            SrLogger.LogWarning($"BuildImprovements detection failed: {e.Message}");
        }
    }

    private static PropertyInfo? FindProperty(Type type, string name)
    {
        var property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return property != null && property.PropertyType == typeof(Color) && property.GetMethod != null
            ? property
            : null;
    }

    private static MemberInfo? FindValidityMember(Type type)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        var property = type.GetProperty("CurrentValidity", flags);
        if (property?.PropertyType.IsEnum == true && property.GetMethod != null)
            return property;

        var field = type.GetField("CurrentValidity", flags);
        return field?.FieldType.IsEnum == true
            ? field
            : null;
    }
}
