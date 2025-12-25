using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;

namespace SR2MP;

public static class Extensions
{
    public static bool TryGetNetworkComponent(this IdentifiableModel actor, out NetworkActor component)
    {
        var gameObject = actor.GetGameObject();
        
        if (gameObject == null)
        {
            component = null!;
            return false;
        }
        
        return gameObject.TryGetComponent<NetworkActor>(out component);
    }
}