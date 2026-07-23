using Il2CppMonomiPark.SlimeRancher.DataModel;

namespace SR2MP.Shared.Managers;

internal static class NetworkDepositorManager
{
    private static readonly Dictionary<string, int> PendingStates = new();

    static NetworkDepositorManager()
    {
        Main.Server.OnServerStarted +=      PendingStates.Clear;
        Main.Client.OnConnected     += _ => PendingStates.Clear();
        Main.Client.OnDisconnected  +=      PendingStates.Clear;
    }

    /// <summary>
    /// Plort Depositors received that have no model.
    /// </summary>
    internal static IReadOnlyDictionary<string, int> PendingDepositorStates => PendingStates;

    private static string? ResolveId(PlortDepositor depositor, PlortDepositorModel? model)
    {
        var depositors = SceneContext.Instance.GameModel?.depositors;
        if (depositors == null)
            return null;

        var id = depositor.Id;
        if (!string.IsNullOrEmpty(id) && depositors.ContainsKey(id))
            return id;

        if (model == null)
            return null;

        foreach (var pair in depositors)
        {
            if (pair.value != null && pair.value.Pointer == model.Pointer)
                return pair.key;
        }

        return null;
    }

    /// <summary>
    /// Applies a received fill amount
    /// or caches it when the depositor's model does not exist yet.
    /// </summary>
    internal static void ApplyState(string id, int amountDeposited)
    {
        var depositors = SceneContext.Instance.GameModel?.depositors;
        if (depositors == null || !depositors.TryGetValue(id, out var model) || model == null)
        {
            PendingStates[id] = amountDeposited;
            return;
        }

        PendingStates.Remove(id);

        if (model.AmountDeposited == amountDeposited)
            return;

        model.AmountDeposited = amountDeposited;
        model.NotifyParticipants();

        if (!model._gameObject)
            return;

        var depositor = model._gameObject.GetComponent<PlortDepositor>();
        if (depositor)
            depositor.OnFilledChangedFromModel();
    }

    /// <summary>
    /// Applies a pending state to a depositor that just got a model.
    /// </summary>
    internal static void ApplyPendingState(PlortDepositor depositor, PlortDepositorModel? model)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;
        if (model == null || PendingStates.Count == 0) return;

        var id = ResolveId(depositor, model);
        if (id == null || !PendingStates.Remove(id, out var amountDeposited))
            return;

        if (model.AmountDeposited == amountDeposited)
            return;

        HandlingPacket = true;
        try
        {
            model.AmountDeposited = amountDeposited;
            depositor.OnFilledChangedFromModel();
            model.NotifyParticipants();
        }
        finally
        {
            HandlingPacket = false;
        }
    }
}
