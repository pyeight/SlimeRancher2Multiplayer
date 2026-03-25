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

    public static int GetPersistentID(SceneGroup sceneGroup)
        => GameContext.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(sceneGroup);
}