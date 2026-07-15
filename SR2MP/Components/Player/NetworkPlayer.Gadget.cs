using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using Il2CppMonomiPark.SlimeRancher.Rendering.Pass;
using SR2MP.Client.Models;
using SR2MP.Packets.Player;
using SR2MP.Shared.Managers;
using SR2MP.Shared.ModSupport;
using static SR2MP.Shared.Utils.Timers;

using Il2CppInterop.Runtime.Attributes;
using UnityEngine.Rendering.HighDefinition;

namespace SR2MP.Components.Player;

internal partial class NetworkPlayer
{
    [HideFromIl2Cpp]
    public event Action<bool>? OnNetworkGadgetModeChanged;
    [HideFromIl2Cpp]
    public event Action<int>? OnNetworkGadgetIDChanged;

    // private bool InGadgetMode => IsLocal ? PlayerItemController._gadgetItem.enabled : OnlineGadgetMode;

    public GadgetPlacementValidity OnlinePlacementValidity;
    private GadgetPlacementValidity? CachedPlacementValidity;

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

    private static readonly int OverlayValidColor = Shader.PropertyToID("_OverlayValidColor");
    private static readonly int OverlayInvalidColor = Shader.PropertyToID("_OverlayInvalidColor");
    private static readonly int Opacity = Shader.PropertyToID("_Opacity");
    
    private static readonly HashSet<int> ActiveRemoteGadgets = new();
    private static float cachedOverlayOpacity = 0.575f;

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
            UpdateFootprintMaterial();
            UpdateOnlineOverlay();
        }
    }

    private void UpdateOnlineOverlay()
    {
        var gadgetItem = GetPlayerItemController()?._gadgetItem;
        if (gadgetItem == null)
            return;

        ActiveRemoteGadgets.Add(GetInstanceID());

        var volume = gadgetItem._gadgetOverlayCustomPassVolume;
        var overlayPass = GetOverlayPass(gadgetItem);
        var placementMaterial = overlayPass?._gadgetPlacementMaterial;
        if (volume == null || overlayPass == null || placementMaterial == null)
            return;
        
        if (gadgetItem.enabled)
        {
            if (placementMaterial.HasProperty(Opacity))
            {
                var currentOpacity = placementMaterial.GetFloat(Opacity);
                if (currentOpacity > 0.01f)
                    cachedOverlayOpacity = currentOpacity;
            }

            return;
        }

        if (!volume.gameObject.activeSelf)
            volume.gameObject.SetActive(true);

        if (placementMaterial.HasProperty(Opacity))
            placementMaterial.SetFloat(Opacity, cachedOverlayOpacity);

        overlayPass.IsGadgetValid =
            OnlinePlacementValidity != GadgetPlacementValidity.Invalid ? 1f : 0f;

        ApplyPlacementImprovementsColor(overlayPass);
    }

    private void ReleaseOverlay()
    {
        ActiveRemoteGadgets.Remove(GetInstanceID());
        if (ActiveRemoteGadgets.Count > 0)
            return;

        if (SceneContext.Instance?.Player == null)
            return;

        var gadgetItem = GetPlayerItemController()?._gadgetItem;
        if (gadgetItem?.enabled != false)
            return;

        var volume = gadgetItem._gadgetOverlayCustomPassVolume;
        if (volume?.gameObject.activeSelf == true)
            volume.gameObject.SetActive(false);

        var placementMaterial = GetOverlayPass(gadgetItem)?._gadgetPlacementMaterial;
        if (placementMaterial?.HasProperty(Opacity) == true)
            placementMaterial.SetFloat(Opacity, 0f);
    }

    private void ApplyPlacementImprovementsColor(GadgetsOverlayModeCustomPass overlayPass)
    {
        if (!PlacementImprovementsIntegration.TryGetPlacementColor(OnlinePlacementValidity, out var color))
            return;

        var placementMaterial = overlayPass._gadgetPlacementMaterial;
        if (placementMaterial == null)
            return;
        
        var colorId = OnlinePlacementValidity == GadgetPlacementValidity.Invalid
            ? OverlayInvalidColor
            : OverlayValidColor;

        color.a = placementMaterial.GetColor(colorId).a;
        placementMaterial.SetColor(colorId, color);
    }

    private void UpdateFootprintMaterial()
    {
        if (CachedPlacementValidity == OnlinePlacementValidity)
            return;

        var material = GetFootprintMaterial(OnlinePlacementValidity != GadgetPlacementValidity.Invalid);
        if (material == null)
            return;

        CachedPlacementValidity = OnlinePlacementValidity;
        FootprintRendererInstance!.material = material;

        if (PlacementImprovementsIntegration.TryGetPlacementColor(OnlinePlacementValidity, out var color))
        {
            var footprintMaterial = FootprintRendererInstance.material;
            color.a = footprintMaterial.color.a;
            footprintMaterial.color = color;
        }
    }
    
    private static GadgetsOverlayModeCustomPass? GetOverlayPass(GadgetItem gadgetItem)
    {
        var customPass = gadgetItem._gadgetsOverlayCustomPass;
        if (customPass != null)
            return customPass;

        var volume = gadgetItem._gadgetOverlayCustomPassVolume;
        var customPasses = volume?.customPasses;
        if (customPasses == null)
            return null;

        foreach (var pass in customPasses)
        {
            var overlayPass = pass?.TryCast<GadgetsOverlayModeCustomPass>();
            if (overlayPass != null)
                return overlayPass;
        }

        return null;
    }

    private void CleanupGadgetPreview()
    {
        if (FootprintPrefabInstance)
            Destroy(FootprintPrefabInstance);

        FootprintPrefabInstance = null;
        FootprintRendererInstance = null;
        PlaceholderGadgetPrefabInstance = null;
        CachedPlacementValidity = null;

        ReleaseOverlay();
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

        var validity = (controller._gadgetItem._isPlacementValid &&
                        !controller._gadgetItem._isPlacementBlocked)
                        || gadgetID == -1
            ? GadgetPlacementValidity.Valid
            : GadgetPlacementValidity.Invalid;
        
        if (gadgetID != -1 && PlacementImprovementsIntegration.TryGetCurrentValidity(out var modValidity))
            validity = modValidity;

        var packet = new PlayerGadgetUpdatePacket
        {
            Enabled = true,
            PlayerId = LocalID,
            Position = FootprintPrefabInstance.transform.position,
            Rotation = FootprintPrefabInstance.transform.rotation,
            GadgetLocalRotation = gadgetLocalRotation,
            CurrentGadget = gadgetID,
            Validity = validity,
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
        OnlinePlacementValidity = player.OnlineGadgetValidity;
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
            CachedPlacementValidity = null;
        }
        else
        {
            CleanupGadgetPreview();
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

    private void SetHeldGadget(GadgetItem gadget, GadgetDefinition? gadgetDefinition)
    {
        Destroy(PlaceholderGadgetPrefabInstance);
        PlaceholderGadgetPrefabInstance = null;

        if (!gadgetDefinition)
            return;

        var gadgetDefinitionToPlace = gadget.GetGadgetDefinitionToPlace(gadgetDefinition);
        var prefab = gadgetDefinitionToPlace.prefab;
        var footprintTransform = FootprintPrefabInstance!.transform;

        PlaceholderGadgetPrefabInstance = gadget.CopyPlaceholderGameObject(prefab, footprintTransform);
        PlaceholderGadgetPrefabInstance.SetActive(false);

        gadget.CopyMeshComponents(prefab);
        gadget.CopyGadgetComponents(prefab);
        gadget.CopySpecialComponents(prefab);

        PlaceholderGadgetPrefabInstance.SetActive(true);

        var overlayPass = GetOverlayPass(gadget);
        var placementLayer = overlayPass != null ? GetFirstLayer(overlayPass.GadgetPlacementLayerMask.value) : -1;
        if (overlayPass != null && placementLayer != -1)
        {
            var convertMask = 1 | overlayPass.GadgetLayerMask.value;
            SetPlacementLayerRecursively(PlaceholderGadgetPrefabInstance.transform, placementLayer, convertMask);
        }

        PlaceholderGadgetPrefabInstance.transform.parent = footprintTransform;
        PlaceholderGadgetPrefabInstance.transform.localPosition = Vector3.zero;

        ApplyGadgetLocalRotation();

        DontDestroyOnLoad(PlaceholderGadgetPrefabInstance);
    }

    private static int GetFirstLayer(int mask)
    {
        for (var layer = 0; layer < 32; layer++)
        {
            if ((mask & (1 << layer)) != 0)
                return layer;
        }

        return -1;
    }

    private static void SetPlacementLayerRecursively(Transform target, int placementLayer, int convertMask)
    {
        var targetObject = target.gameObject;
        if ((convertMask & (1 << targetObject.layer)) != 0)
            targetObject.layer = placementLayer;

        for (var i = 0; i < target.childCount; i++)
            SetPlacementLayerRecursively(target.GetChild(i), placementLayer, convertMask);
    }
}