using System.Diagnostics;
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
    /*public static long SR2MPMax(this IEnumerable<long> source)
    {
        if (source == null)
        {
            var stack = new StackTrace();
            SrLogger.LogError($"paramater 'source' is null!\n{stack}");
            return 0;
        }

        long? value;
        using (IEnumerator<long> e = source.GetEnumerator())
        {
            if (!e.MoveNext())
            {
                var stack = new StackTrace();
                SrLogger.LogError($"paramater 'source' is empty!\n{stack}");
                return 0;
            }

            value = e.Current;
            while (e.MoveNext())
            {
                long? x = e.Current;
                if (x > value)
                {
                    value = x;
                }
            }
        }

        if (value == null)
        {
            var stack = new StackTrace();
            SrLogger.LogError($"Return value was null!\n{stack}");
            return 0;
        }

        return (long)value;
    }*/
}