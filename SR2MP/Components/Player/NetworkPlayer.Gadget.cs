using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using Il2CppMonomiPark.SlimeRancher.Util.Extensions;
using SR2MP.Client.Models;
using SR2MP.Packets.Player;
using SR2MP.Shared.Managers;
using static SR2MP.Shared.Utils.Timers;

namespace SR2MP.Components.Player;

internal partial class NetworkPlayer
{
    public event Action<bool>? OnNetworkGadgetModeChanged;
    public event Action<int>? OnNetworkGadgetIDChanged;

    // private bool InGadgetMode => IsLocal ? PlayerItemController._gadgetItem.enabled : OnlineGadgetMode;

    public bool OnlinePlacementValid;

    public bool OnlineGadgetMode;
    public bool CachedOnlineGadgetMode;

    public int OnlineGadgetID;
    public int CachedOnlineGadgetID;

    public Vector3 NextGadgetPosition;
    public Vector3 PrevGadgetPosition;
    public Quaternion NextGadgetRotation;
    public Quaternion PrevGadgetRotation;
    public Quaternion OnlineGadgetLocalRotation;

    public PlayerItemController PlayerItemController;
    public GameObject? FootprintPrefabInstance;
    public Renderer? FootprintRendererInstance;
    public GameObject? PlaceholderGadgetPrefabInstance;

    private float interpolationStartGadget;
    private float interpolationEndGadget;
    private float transformTimerGadget = PlayerTimer;

    private void AwakeGadgetMode()
    {
        PlayerItemController = SceneContext.Instance.Player.GetComponent<PlayerItemController>();

        OnNetworkGadgetModeChanged += OnGadgetModeChanged;
        OnNetworkGadgetIDChanged += OnGadgetIDChanged;
    }

    private void ApplyGadgetLocalRotation()
    {
        if (!PlaceholderGadgetPrefabInstance) return;
        var gadgetObj = PlaceholderGadgetPrefabInstance!.GetComponentInChildren<Gadget>();
        if (gadgetObj)
            gadgetObj.transform.localRotation = OnlineGadgetLocalRotation;
    }

    private void UpdateGadgetInterpolation()
    {
        if (!FootprintPrefabInstance)
            return;

        var t = Mathf.InverseLerp(interpolationStartGadget, interpolationEndGadget, UnityEngine.Time.unscaledTime);
        t = Mathf.Clamp01(t);

        FootprintPrefabInstance!.transform.position = Vector3.Lerp(PrevGadgetPosition, NextGadgetPosition, t);
        FootprintPrefabInstance!.transform.rotation = Quaternion.Slerp(PrevGadgetRotation, NextGadgetRotation, t);

        ApplyGadgetLocalRotation();
    }

    private void UpdateGadgetMode()
    {
        if (FootprintPrefabInstance && !IsLocal)
            UpdateGadgetInterpolation();

        transformTimerGadget -= UnityEngine.Time.unscaledDeltaTime;
        if (transformTimerGadget >= 0f)
            return;

        if (IsLocal)
            UpdateLocalGadgetMode();
        else
            UpdateOnlineGadgetMode();
    }

    private Material GetFootprintMaterial(bool isValidPlacement)
    {
        return isValidPlacement
            ? PlayerItemController._gadgetItem._gadgetItemMetadata.GadgetFootprintValidMaterial
            : PlayerItemController._gadgetItem._gadgetItemMetadata.GadgetFootprintInvalidMaterial;
    }

    private void UpdateOnlineGadgetMode()
    {
        interpolationStartGadget = UnityEngine.Time.unscaledTime;
        interpolationEndGadget = UnityEngine.Time.unscaledTime + PlayerTimer;

        if (CachedOnlineGadgetMode != OnlineGadgetMode)
            OnNetworkGadgetModeChanged?.Invoke(OnlineGadgetMode);
        
        CachedOnlineGadgetMode = OnlineGadgetMode;

        if (CachedOnlineGadgetID != OnlineGadgetID)
            OnNetworkGadgetIDChanged?.Invoke(OnlineGadgetID);
        
        CachedOnlineGadgetID = OnlineGadgetID;

        if (FootprintPrefabInstance)
            FootprintRendererInstance!.material = GetFootprintMaterial(OnlinePlacementValid);
    }

    private void UpdateLocalGadgetMode()
    {
        FootprintPrefabInstance = PlayerItemController._gadgetItem._gadgetFootprintInstance;

        if (!FootprintPrefabInstance)
        {
            var packet2 = new PlayerGadgetUpdatePacket
            {
                Enabled = false,
                PlayerId = Main.Client.IsConnected ? Main.Client.PlayerId : (Main.Server.IsRunning ? Main.Server.PlayerId : string.Empty),
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                GadgetLocalRotation = Quaternion.identity,
                CurrentGadget = -1,
                ValidPlacement = false,
            };

            Main.SendToAllOrServer(packet2);
            return;
        }

        var gadget = PlayerItemController._gadgetItem._heldGadget;
        var gadgetID =
            gadget
            ? NetworkActorManager.GetPersistentID(gadget.Cast<IdentifiableType>())
            : -1;

        var gadgetLocalRotation = Quaternion.identity;
        var gadgetObj = FootprintPrefabInstance.GetComponentInChildren<Gadget>();
        if (gadgetObj)
            gadgetLocalRotation = gadgetObj.transform.localRotation;

        var packet = new PlayerGadgetUpdatePacket
        {
            Enabled = true,
            PlayerId = Main.Client.IsConnected ? Main.Client.PlayerId : (Main.Server.IsRunning ? Main.Server.PlayerId : string.Empty),
            Position = FootprintPrefabInstance.transform.position,
            Rotation = FootprintPrefabInstance.transform.rotation,
            GadgetLocalRotation = gadgetLocalRotation,
            CurrentGadget = gadgetID,
            ValidPlacement = (PlayerItemController._gadgetItem._isPlacementValid &&
                              !PlayerItemController._gadgetItem._isPlacementBlocked)
                              || gadgetID == -1,
        };

        Main.SendToAllOrServer(packet);
    }

    private void OnGadgetUpdate(string playerId, RemotePlayer player)
    {
        if (ID != playerId)
            return;
        
        OnGadgetPositionReceived(
            player.OnlineGadgetPosition,
            player.OnlineGadgetRotation,
            player.OnlineGadgetLocalRotation);

        OnlineGadgetID = player.OnlineGadgetID;
        OnlinePlacementValid = player.OnlineGadgetValid;
        OnlineGadgetMode = player.OnlineGadgetMode;
    }
    
    public void OnGadgetPositionReceived(Vector3 newPosition, Quaternion newRotation, Quaternion newLocalRotation)
    {
        PrevGadgetPosition = FootprintPrefabInstance?.transform.position ?? newPosition;
        PrevGadgetRotation = FootprintPrefabInstance?.transform.rotation ?? newRotation;

        NextGadgetPosition = newPosition;
        NextGadgetRotation = newRotation;
        OnlineGadgetLocalRotation = newLocalRotation;

        ApplyGadgetLocalRotation();
    }

    private void OnGadgetModeChanged(bool newMode)
    {
        if (newMode)
        {
            var footprintPrefab = PlayerItemController._gadgetItem._gadgetItemMetadata.GadgetFootprintPrefab;
            var footprintRendererPrefab =
                PlayerItemController._gadgetItem._gadgetItemMetadata.GadgetFootprintRendererPrefab;
            FootprintPrefabInstance = Instantiate(footprintPrefab);
            DontDestroyOnLoad(FootprintPrefabInstance);

            FootprintPrefabInstance.transform.position = NextGadgetPosition;
            FootprintPrefabInstance.transform.rotation = NextGadgetRotation;
            PrevGadgetPosition = NextGadgetPosition;
            PrevGadgetRotation = NextGadgetRotation;
            var renderer = Instantiate(footprintRendererPrefab, FootprintPrefabInstance.transform, false);
            FootprintRendererInstance = renderer.GetComponent<MeshRenderer>();
        }
        else
        {
            Destroy(FootprintPrefabInstance);
            FootprintPrefabInstance = null;
        }
    }

    private void OnGadgetIDChanged(int gadgetID)
    {
        if (gadgetID == -1)
        {
            SetHeldGadget(PlayerItemController._gadgetItem, null);
            return;
        }

        if (!ActorManager.ActorTypes.TryGetValue(gadgetID, out var type))
        {
            SrLogger.LogWarning($"OnGadgetIDChanged: no actor type found for id {gadgetID}");
            return;
        }

        var definition = type.TryCast<GadgetDefinition>();
        if (!definition)
        {
            SrLogger.LogWarning("OnGadgetIDChanged: Could not Cast for a GadgetDefinition!");
            return;
        }

        SetHeldGadget(PlayerItemController._gadgetItem, definition);
    }

    public void SetHeldGadget(GadgetItem self, GadgetDefinition? gadgetDefinition)
    {
        Destroy(PlaceholderGadgetPrefabInstance);
        PlaceholderGadgetPrefabInstance = null;

        if (!gadgetDefinition)
            return;

        var gadgetDefinitionToPlace = self.GetGadgetDefinitionToPlace(gadgetDefinition);
        var prefab = gadgetDefinitionToPlace.prefab;
        var footprintTransform = FootprintPrefabInstance!.transform;

        PlaceholderGadgetPrefabInstance = self.CopyPlaceholderGameObject(prefab, footprintTransform);
        PlaceholderGadgetPrefabInstance.SetActive(false);

        self.CopyMeshComponents(prefab);
        self.CopyGadgetComponents(prefab);
        self.CopySpecialComponents(prefab);

        PlaceholderGadgetPrefabInstance.SetActive(true);

        self.SetGadgetLayerRecursively(prefab.transform, prefab.SRGetComponent<Gadget>());

        PlaceholderGadgetPrefabInstance.transform.parent = footprintTransform;
        PlaceholderGadgetPrefabInstance.transform.localPosition = Vector3.zero;

        ApplyGadgetLocalRotation();

        DontDestroyOnLoad(PlaceholderGadgetPrefabInstance);
    }
}