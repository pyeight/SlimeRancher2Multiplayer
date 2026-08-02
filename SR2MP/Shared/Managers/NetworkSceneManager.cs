using Il2CppMonomiPark.SlimeRancher.SceneManagement;

namespace SR2MP.Shared.Managers;

internal static class NetworkSceneManager
{
    private static readonly Dictionary<int, SceneGroup> AllSceneGroups = new();

    internal static void Initialize(GameContext context)
    {
        AllSceneGroups.Clear();

        var translator = context.AutoSaveDirector._saveReferenceTranslation._sceneGroupTranslation;

        foreach (var group in translator.RawLookupDictionary)
            AllSceneGroups.Add(translator.InstanceLookupTable._reverseIndex[group.Key], group.Value);
    }

    public static SceneGroup GetSceneGroup(int sceneGroupId) => AllSceneGroups[sceneGroupId];

    public static IEnumerable<KeyValuePair<int, SceneGroup>> GetAllSceneGroups() => AllSceneGroups;

    /// <summary>
    /// Whether a scene group is the one currently loaded
    /// </summary>
    public static bool IsSceneGroupLoaded(SceneGroup? sceneGroup)
    {
        if (sceneGroup == null)
            return true;

        try
        {
            var current = SystemContext.Instance?.SceneLoader?.CurrentSceneGroup;
            if (current == null)
                return true;

            var currentId = GetPersistentID(current);
            var targetId = GetPersistentID(sceneGroup);

            if (currentId < 0 || targetId < 0)
                return true;

            return currentId == targetId;
        }
        catch (Exception)
        {
            return true;
        }
    }

    /// <summary>
    /// Resolves a scene group's save persistence id.
    /// </summary>
    /// <returns>The persistence id, or -1 for scene groups outside the translation table (menu, error handling).</returns>
    public static int GetPersistentID(SceneGroup sceneGroup)
    {
        try
        {
            return GameContext.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(sceneGroup);
        }
        catch (Exception)
        {
            return -1;
        }
    }
}