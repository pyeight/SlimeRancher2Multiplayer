using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using Il2CppMonomiPark.SlimeRancher.Util.Extensions;
using SR2MP.Client.Models;
using SR2MP.Packets.Player;
using SR2MP.Shared.Managers;
using static SR2MP.Shared.Utils.Timers;

using Il2CppInterop.Runtime.Attributes;

namespace SR2MP.Components.Player;

internal partial class NetworkPlayer
{
    [HideFromIl2Cpp]
    public event Action<bool>? OnNetworkGadgetModeChanged;
    [HideFromIl2Cpp]
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
        OnNetworkGadgetModeChanged += OnGadgetModeChanged;
        OnNetworkGadgetIDChanged += OnGadgetIDChanged;
    }

    private PlayerItemController? GetPlayerItemController()
    {
        if (!PlayerItemController)
            PlayerItemController = SceneContext.Instance.Player.GetComponent<PlayerItemController>();
        
        return PlayerItemController;
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

    private Material? GetFootprintMaterial(bool isValidPlacement)
    {
        var controller = GetPlayerItemController();

        return isValidPlacement
            ? controller!._gadgetItem._gadgetItemMetadata.GadgetFootprintValidMaterial
            : controller!._gadgetItem._gadgetItemMetadata.GadgetFootprintInvalidMaterial;
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
        {
            var material = GetFootprintMaterial(OnlinePlacementValid);
            if (material != null)
                FootprintRendererInstance!.material = material;
        }
    }

    private void UpdateLocalGadgetMode()
    {
        var controller = GetPlayerItemController();

        FootprintPrefabInstance = controller!._gadgetItem._gadgetFootprintInstance;

        if (!FootprintPrefabInstance)
        {
            var packet2 = new PlayerGadgetUpdatePacket
            {
                Enabled = false,
                PlayerId = LocalID,
            };

            Main.SendToAllOrServer(packet2);
            return;
        }

        var gadget = controller._gadgetItem._heldGadget;
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
            PlayerId = LocalID,
            Position = FootprintPrefabInstance.transform.position,
            Rotation = FootprintPrefabInstance.transform.rotation,
            GadgetLocalRotation = gadgetLocalRotation,
            CurrentGadget = gadgetID,
            ValidPlacement = (controller._gadgetItem._isPlacementValid &&
                              !controller._gadgetItem._isPlacementBlocked)
                              || gadgetID == -1,
        };

        Main.SendToAllOrServer(packet);
    }

    [HideFromIl2Cpp]
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
        if (FootprintPrefabInstance != null && FootprintPrefabInstance)
        {
            PrevGadgetPosition = FootprintPrefabInstance.transform.position;
            PrevGadgetRotation = FootprintPrefabInstance.transform.rotation;
        }
        else
        {
            PrevGadgetPosition = newPosition;
            PrevGadgetRotation = newRotation;
        }

        NextGadgetPosition = newPosition;
        NextGadgetRotation = newRotation;
        OnlineGadgetLocalRotation = newLocalRotation;

        ApplyGadgetLocalRotation();
    }

    private void OnGadgetModeChanged(bool newMode)
    {
        if (newMode)
        {
            var controller = GetPlayerItemController();

            var footprintPrefab = controller!._gadgetItem._gadgetItemMetadata.GadgetFootprintPrefab;
            var footprintRendererPrefab =
                controller._gadgetItem._gadgetItemMetadata.GadgetFootprintRendererPrefab;
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
        var controller = GetPlayerItemController();

        if (gadgetID == -1)
        {
            SetHeldGadget(controller!._gadgetItem, null);
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

        SetHeldGadget(controller!._gadgetItem, definition);
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