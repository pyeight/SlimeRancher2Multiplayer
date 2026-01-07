using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;

namespace SR2MP;

public static class Extensions
{
    public static bool TryGetNetworkComponent(this IdentifiableModel actor, out NetworkActor component)
    {
        var gameObject = actor.GetGameObject();

        if (gameObject)
            return gameObject.TryGetComponent(out component);

        component = null!;
        return false;
    }

    public static string ToStringYesOrNo(this bool value)
    {
        switch (value)
        {
            case true:
                return "Yes";
            case false:
                return "No";
        }
    }
}