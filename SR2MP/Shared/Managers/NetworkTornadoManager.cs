using System.Net;
using Il2CppMonomiPark.SlimeRancher.Weather.Activity;
using SR2MP.Packets.World;

namespace SR2MP.Shared.Managers;

internal static class NetworkTornadoManager
{
    private const float RenderDistance = 240f;

    internal static SpawnTornadoActivity? LastActivity;

    private static int currentId = 1;

    internal static readonly Dictionary<int, GameObject> HostTornados = new();
    internal static readonly Dictionary<int, GameObject> ClientTornados = new();
    internal static readonly Dictionary<int, PendingTornado> PendingTornados = new();

    internal struct PendingTornado
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float ReceivedAt;
    }

    internal static int IncreaseId() => currentId++;

    internal static bool PlayerInDistance(Vector3 position)
    {
        try
        {
            var player = SceneContext.Instance?.player;
            if (player == null)
                return false;

            return (player.transform.position - position).sqrMagnitude <= RenderDistance * RenderDistance;
        }
        catch
        {
            return false;
        }
    }

    internal static void SpawnTornado(int id, Vector3 position, Quaternion rotation)
    {
        var activity = EnsureActivity();
        if (activity == null)
            return;

        try
        {
            HandlingPacket = true;
            var tornado = activity.Spawn(position, rotation);
            if (tornado)
                ClientTornados[id] = tornado;
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"SpawnTornado: {ex.Message}");
        }
        finally
        {
            HandlingPacket = false;
        }
    }

    internal static void ClientTick()
    {
        if (Main.Server.IsRunning || PendingTornados.Count == 0)
            return;

        List<int>? spawnedTornados = null;
        List<int>? droppedTornados = null;
        var now = Time.unscaledTime;

        foreach (var pair in PendingTornados)
        {
            if (now - pair.Value.ReceivedAt > 90f)
                (droppedTornados ??= new List<int>()).Add(pair.Key);
            else if (PlayerInDistance(pair.Value.Position))
                (spawnedTornados ??= new List<int>()).Add(pair.Key);
        }

        if (droppedTornados != null)
            foreach (var id in droppedTornados)
                PendingTornados.Remove(id);

        if (spawnedTornados != null)
            foreach (var id in spawnedTornados)
            {
                var tornado = PendingTornados[id];
                PendingTornados.Remove(id);
                SpawnTornado(id, tornado.Position, tornado.Rotation);
            }
    }
    
    internal static void HostTick()
    {
        if (!Main.Server.IsRunning || HostTornados.Count == 0)
            return;

        List<int>? disappeared = null;
        foreach (var pair in HostTornados)
        {
            if (!pair.Value)
                (disappeared ??= new List<int>()).Add(pair.Key);
        }

        if (disappeared == null)
            return;

        foreach (var id in disappeared)
        {
            HostTornados.Remove(id);
            Main.Server.SendToAll(new TornadoSpawnPacket { TornadoId = id, Despawn = true });
        }
    }

    internal static void SendTornadosTo(IPEndPoint client)
    {
        try
        {
            List<int>? disappeared = null;
            foreach (var pair in HostTornados)
                if (!pair.Value) (disappeared ??= new List<int>()).Add(pair.Key);
            if (disappeared != null) foreach (var id in disappeared) HostTornados.Remove(id);

            foreach (var pair in HostTornados)
            {
                Main.Server.SendToClient(new TornadoSpawnPacket
                {
                    TornadoId = pair.Key,
                    Position = pair.Value.transform.position,
                    Rotation = pair.Value.transform.rotation
                }, client);
            }
            
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"SendTornadosTo: {ex.Message}");
        }
    }

    private static SpawnTornadoActivity? EnsureActivity()
    {
        if (LastActivity != null)
            return LastActivity;

        try
        {
            var allTornados = Resources.FindObjectsOfTypeAll<SpawnTornadoActivity>();
            if (allTornados?.Length > 0)
                LastActivity = allTornados[0];
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"EnsureActivity: {ex.Message}");
        }

        return LastActivity;
    }
}
