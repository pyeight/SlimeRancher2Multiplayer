using Il2CppMonomiPark.SlimeRancher.DataModel;

namespace SR2MP.Shared.Managers;

/// <summary>
/// Applies puzzle slot fill states.
/// States for slots whose model is not registered yet
/// are kept pending and applied once the slot has a model.
/// </summary>
internal static class NetworkPuzzleSlotManager
{
    private static readonly Dictionary<string, bool> PendingStates = new();

    static NetworkPuzzleSlotManager()
    {
        Main.Server.OnServerStarted +=      PendingStates.Clear;
        Main.Client.OnConnected     += _ => PendingStates.Clear();
        Main.Client.OnDisconnected  +=      PendingStates.Clear;
    }

    /// <summary>
    /// Fill states received from the network that have no model yet.
    /// </summary>
    internal static IReadOnlyDictionary<string, bool> PendingSlotStates => PendingStates;

    /// <summary>
    /// This is entirely more than what we should need for this
    /// To make quadruple sure puzzle slots really are synced, we work with pointers
    /// </summary>
    internal static string? ResolveId(PuzzleSlot slot, PuzzleSlotModel? model)
    {
        var slots = SceneContext.Instance.GameModel?.slots;
        if (slots == null)
            return null;

        var id = slot.Id;
        if (!string.IsNullOrEmpty(id) && slots.ContainsKey(id))
            return id;

        if (model == null)
            return null;

        foreach (var pair in slots)
        {
            if (pair.value != null && pair.value.Pointer == model.Pointer)
                return pair.key;
        }

        return null;
    }

    /// <summary>
    /// Applies a received fill state,
    /// or remembers it when the slots model does not exist yet.
    /// </summary>
    internal static void ApplyState(string id, bool filled)
    {
        var slots = SceneContext.Instance.GameModel?.slots;
        if (slots == null || !slots.TryGetValue(id, out var model) || model == null)
        {
            PendingStates[id] = filled;
            return;
        }

        PendingStates.Remove(id);

        if (model.filled == filled)
            return;

        model.filled = filled;

        // not loaded, let the game handle the visual
        if (!model.gameObj)
            return;

        var slot = model.gameObj.GetComponent<PuzzleSlot>();
        if (slot && filled)
            slot!.ActivateOnFill();

        model.NotifyParticipants();
    }

    /// <summary>
    /// Applies a pending fill state to a slot that just got a model.
    /// </summary>
    internal static void ApplyPendingState(PuzzleSlot slot, PuzzleSlotModel? model)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;
        if (model == null || PendingStates.Count == 0) return;

        var id = ResolveId(slot, model);
        if (id == null || !PendingStates.Remove(id, out var filled))
            return;

        if (model.filled == filled)
            return;

        HandlingPacket = true;
        try
        {
            model.filled = filled;

            if (filled)
                slot.ActivateOnFill();

            model.NotifyParticipants();
        }
        finally
        {
            HandlingPacket = false;
        }
    }
}
