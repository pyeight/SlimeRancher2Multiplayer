using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using UnityEngine.SceneManagement;

namespace SR2MP.Shared.Managers;

public static class NetworkSceneManager
{
    private static Dictionary<int, SceneGroup> allSceneGroups = new();

    internal static void Initialize(GameContext context)
    {
        allSceneGroups.Clear();
        
        var translator = context.AutoSaveDirector._saveReferenceTranslation._sceneGroupTranslation;
        
        foreach (var group in translator.RawLookupDictionary)
            allSceneGroups.Add(translator.InstanceLookupTable._reverseIndex[group.Key], group.Value);
    }
    
    public static SceneGroup GetSceneGroup(int sceneGroupId) => allSceneGroups[sceneGroupId];

    public static int GetPersistentID(SceneGroup sceneGroup)
        => GameContext.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(sceneGroup);
}