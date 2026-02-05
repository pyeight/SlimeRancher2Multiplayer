using Il2CppInterop.Runtime.InteropTypes;

namespace SR2MP.Shared.Utils;

public static class Helpers
{
    public static bool TryCast<T>(this Il2CppObjectBase baseObj, out T castedObj) where T : Il2CppObjectBase => (castedObj = baseObj.TryCast<T>()!) != null;
}