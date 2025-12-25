using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Player;

namespace SR2MP.Shared.Managers;

public class NetworkActorManager
{
    public readonly Dictionary<long, IdentifiableModel> Actors = new Dictionary<long, IdentifiableModel>();
    
    public readonly Dictionary<int, IdentifiableType> ActorTypes = new Dictionary<int, IdentifiableType>();

    public int GetPersistentID(IdentifiableType type)
        => GameContext.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(type);

    internal void Initialize(GameContext context)
    {
        ActorTypes.Clear();
        Actors.Clear();
        
        foreach (var type in context.AutoSaveDirector._saveReferenceTranslation._identifiableTypeLookup)
        {
            ActorTypes.TryAdd(GetPersistentID(type.value), type.value);
        }
    }
}