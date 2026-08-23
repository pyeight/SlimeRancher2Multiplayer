using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.TreasurePod;
using Pod = Il2Cpp.TreasurePod;

namespace SR2MP.Shared.Managers;

internal static class NetworkTreasurePodManager
{
    private static readonly Dictionary<string, Pod.State> PendingStates = new();

    static NetworkTreasurePodManager()
    {
        Main.Server.OnServerStarted +=      PendingStates.Clear;
        Main.Client.OnConnected     += _ => PendingStates.Clear();
        Main.Client.OnDisconnected  +=      PendingStates.Clear;
    }

    /// <summary>
    /// Treasure Pod states received for pods that have no model.
    /// </summary>
    internal static IReadOnlyDictionary<string, Pod.State> PendingPodStates => PendingStates;

    /// <summary>
    /// Broadcasts the state a pod just entered locally.
    /// </summary>
    internal static void Broadcast(string id, Pod.State state)
    {
        if (string.IsNullOrEmpty(id)) return;

        Main.SendToAllOrServer(new TreasurePodPacket { ID = id, State = state });
    }

    /// <summary>
    /// Applies a received state, or caches it when the pod's model does not exist yet.
    /// </summary>
    internal static void ApplyState(string id, Pod.State state)
    {
        if (string.IsNullOrEmpty(id)) return;

        var pods = SceneContext.Instance.GameModel?.pods;
        if (pods == null || !pods.TryGetValue(id, out var model) || model == null)
        {
            PendingStates[id] = state;
            return;
        }

        PendingStates.Remove(id);
        Apply(model, state, playOpening: true);
    }

    /// <summary>
    /// Applies a cached state to a pod that just got a model.
    /// </summary>
    internal static void ApplyPendingState(string id, TreasurePodModel? model)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;
        if (model == null || PendingStates.Count == 0) return;
        if (string.IsNullOrEmpty(id) || !PendingStates.Remove(id, out var state)) return;
        
        Apply(model, state, playOpening: false);
    }

    private static void Apply(TreasurePodModel model, Pod.State state, bool playOpening)
    {
        if (model.state == null || model.state.Value == state) return;

        HandlingPacket = true;
        try
        {
            model.state.Set(state);

            if (playOpening)
                PlayOpening(model, state);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"NetworkTreasurePodManager: failed to apply state {state}: {ex.Message}");
        }
        finally
        {
            HandlingPacket = false;
        }
    }
    
    private static void PlayOpening(TreasurePodModel model, Pod.State state)
    {
        if (state != Pod.State.OPEN) return;

        var podObj = model.gameObj;
        if (!podObj) return;

        var pod = podObj.GetComponent<Pod>();
        if (!pod) return;

        pod._nextUpdateImmediate = false;
        pod._forceUpdate = true;

        var podTransform = pod.transform;

        if (pod.OpenCue)
            SECTR_AudioSystem.Play(pod.OpenCue, podTransform.position, false);

        if (pod.OpenFX)
            FXHelpers.SpawnAndPlayFX(pod.OpenFX, podTransform.position, podTransform.rotation);
    }
}