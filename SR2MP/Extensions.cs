using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;

namespace SR2MP;

internal static class Extensions
{
    internal static string RandomHexColor()
    {
        var r = UnityEngine.Random.Range(0, 256);
        var g = UnityEngine.Random.Range(0, 256);
        var b = UnityEngine.Random.Range(0, 256);

        return $"{r:X2}{g:X2}{b:X2}";
    }

    internal static bool IsValidHexColor(string? value)
    {
        if (value == null || (value.Length != 6 && value.Length != 8))
            return false;

        foreach (var c in value)
        {
            var isHexDigit = (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
            if (!isHexDigit)
                return false;
        }

        return true;
    }
    
    internal static bool TryGetNetworkComponent(this IdentifiableModel actor, out NetworkActor component)
    {
        var gameObject = actor.GetGameObject();

        if (gameObject)
            return gameObject.TryGetComponent(out component);

        component = null!;
        return false;
    }

    public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
        => obj.GetComponent<T>() ?? obj.AddComponent<T>();
    
    
    public static string ToStringYesOrNo(this bool value) => value ? "Yes" : "No";

    // https://discussions.unity.com/t/how-can-i-get-the-full-path-to-a-gameobject/412
    public static string GetGameObjectPath(this GameObject obj)
    {
        var pathParts = new List<string>();
        var current = obj;
        while (current != null)
        {
            pathParts.Add(current.name);
            var parentTransform = current.transform.parent;
            current = parentTransform != null ? parentTransform.gameObject : null!;
        }
        pathParts.Reverse();
        return "/" + string.Join("/", pathParts);
    }

    /* public static long SR2MPMax(this IEnumerable<long> source)
    {
        if (source == null)
        {
            var stack = new StackTrace();
            SrLogger.LogError($"parameter 'source' is null!\n{stack}");
            return 0;
        }

        long? value;

        using (IEnumerator<long> e = source.GetEnumerator())
        {
            if (!e.MoveNext())
            {
                var stack = new StackTrace();
                SrLogger.LogError($"parameter 'source' is empty!\n{stack}");
                return 0;
            }

            value = e.Current;

            while (e.MoveNext())
            {
                long? x = e.Current;

                if (x > value)
                    value = x;
            }
        }

        if (value == null)
        {
            var stack = new StackTrace();
            SrLogger.LogError($"Return value was null!\n{stack}");
            return 0;
        }

        return (long)value;
    } */
}