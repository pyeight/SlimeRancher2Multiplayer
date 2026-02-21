using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using Il2CppMonomiPark.SlimeRancher.Util.Extensions;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.Player;
using SR2MP.Shared.Managers;
using static SR2MP.Shared.Utils.Timers;

namespace SR2MP.Components.Player;

public partial class NetworkPlayer
{
    public event Action<bool> onNetworkGadgetModeChanged;
    public event Action<int> onNetworkGadgetIDChanged;

    private bool InGadgetMode
    {
        get
        {
            if (IsLocal)
                return playerItemController._gadgetItem.enabled;

            return onlineGadgetMode;
        }
    }

    public bool onlinePlacementValid;
    
    public bool onlineGadgetMode;
    public bool cachedOnlineGadgetMode;

    public int onlineGadgetID;
    public int cachedOnlineGadgetID;

    public Vector3 nextGadgetPosition;
    public Vector3 prevGadgetPosition;
    public Quaternion nextGadgetRotation;
    public Quaternion prevGadgetRotation;

    public PlayerItemController playerItemController;
    public GameObject? footprintPrefabInstance;
    public Renderer? footprintRendererInstance;
    public GameObject? placeholderGadgetPrefabInstance;

    private float interpolationStartGadget;
    private float interpolationEndGadget;
    private float transformTimerGadget = PlayerTimer;
    
    private void AwakeGadgetMode()
    {
        playerItemController = SceneContext.Instance.Player.GetComponent<PlayerItemController>();
        
        onNetworkGadgetModeChanged += OnGadgetModeChanged;
        onNetworkGadgetIDChanged += OnGadgetIDChanged;
    }


    private void UpdateGadgetInterpolation()
    {
        if (!footprintPrefabInstance)
            return;

        //if (interpolationEnd <= interpolationStart)
        //    return;
        
        var t = Mathf.InverseLerp(interpolationStartGadget, interpolationEndGadget, UnityEngine.Time.unscaledTime);
        t = Mathf.Clamp01(t);

        footprintPrefabInstance!.transform.position = Vector3.Lerp(prevGadgetPosition, nextGadgetPosition, t);
        
        if (!placeholderGadgetPrefabInstance)
            return;
        SrLogger.LogMessage($"{ID}: Using {t} as the Lerp slider, the new position position will be:\n{prevGadgetRotation} -> {Quaternion.Slerp(prevGadgetRotation, nextGadgetRotation, t)} -> {nextGadgetRotation}");
        
        placeholderGadgetPrefabInstance!.transform.rotation = Quaternion.Slerp(prevGadgetRotation, nextGadgetRotation, t);
    }

    private void UpdateGadgetMode()
    {
        if (footprintPrefabInstance && !IsLocal)
            UpdateGadgetInterpolation();
        
        transformTimerGadget -= UnityEngine.Time.unscaledDeltaTime;
        if (transformTimerGadget >= 0f)
            return;
        
        
        if (IsLocal)
        {
            UpdateLocalGadgetMode();
            return;
        }
        UpdateOnlineGadgetMode();
    }

    private Material GetFootprintMaterial(bool isValidPlacement)
    {
        return isValidPlacement
            ? playerItemController._gadgetItem._gadgetItemMetadata.GadgetFootprintValidMaterial
            : playerItemController._gadgetItem._gadgetItemMetadata.GadgetFootprintInvalidMaterial;
    }

    private void UpdateOnlineGadgetMode()
    {
        interpolationStartGadget = UnityEngine.Time.unscaledTime;
        interpolationEndGadget = UnityEngine.Time.unscaledTime + PlayerTimer;
        
        if (cachedOnlineGadgetMode != onlineGadgetMode)
        {
            cachedOnlineGadgetMode = onlineGadgetMode;
            onNetworkGadgetModeChanged?.Invoke(cachedOnlineGadgetMode);
        }

        if (cachedOnlineGadgetID != onlineGadgetID)
        {
            cachedOnlineGadgetID = onlineGadgetID;
            onNetworkGadgetIDChanged?.Invoke(cachedOnlineGadgetID);
        }

        if (footprintPrefabInstance)
        {
            //UpdateInterpolation();
            footprintRendererInstance!.material = GetFootprintMaterial(onlinePlacementValid);
        }
    }

    private void UpdateLocalGadgetMode()
    {
        footprintPrefabInstance = playerItemController._gadgetItem._gadgetFootprintInstance;
        
        if (!footprintPrefabInstance)
        {
            var packet2 = new PlayerGadgetUpdatePacket
            {
                Enabled = false,
                PlayerId = ID,
                Position = Vector3.zero,
                Rotation = Vector3.zero,
                CurrentGadget = -1,
                ValidPlacement = false,
            };
        
            Main.SendToAllOrServer(packet2);
            return;
        }
        //if (transformTimer >= 0f)
        //    return;
        
        var gadget = playerItemController._gadgetItem._heldGadget;
        var gadgetID =
            gadget
            ? NetworkActorManager.GetPersistentID(gadget.Cast<IdentifiableType>())
            : -1;
        
        var packet = new PlayerGadgetUpdatePacket
        {
            Enabled = true,
            PlayerId = ID,
            Position = footprintPrefabInstance.transform.position,
            Rotation = playerItemController._gadgetItem._gadgetPlaceholderInstance?.transform.eulerAngles ?? Vector3.zero,
            CurrentGadget = gadgetID,
            ValidPlacement = (playerItemController._gadgetItem._isPlacementValid &&
                              !playerItemController._gadgetItem._isPlacementBlocked)
                             || gadgetID == -1,
        };

        Main.SendToAllOrServer(packet);
    }

    public void OnGadgetPositionReceived(Vector3 newPosition, Vector3 newRotation)
    {
        prevGadgetPosition = footprintPrefabInstance?.transform.position ?? newPosition;

        prevGadgetRotation =  footprintPrefabInstance?.transform.rotation ?? Quaternion.Euler(newRotation);

        nextGadgetPosition = newPosition;
        nextGadgetRotation = Quaternion.Euler(newRotation);
    }

    private void OnGadgetModeChanged(bool newMode)
    {
        if (newMode)
        {
            var footprintPrefab = playerItemController._gadgetItem._gadgetItemMetadata.GadgetFootprintPrefab;
            var footprintRendererPrefab =
                playerItemController._gadgetItem._gadgetItemMetadata.GadgetFootprintRendererPrefab;
            footprintPrefabInstance = Instantiate(footprintPrefab);
            DontDestroyOnLoad(footprintPrefabInstance);

            footprintPrefabInstance.transform.position = nextGadgetPosition;
            //footprintPrefabInstance.transform.rotation = nextGadgetRotation;
            prevGadgetPosition = nextGadgetPosition;
            prevGadgetRotation = nextGadgetRotation;
            var renderer = Instantiate(footprintRendererPrefab, footprintPrefabInstance.transform, false);
            footprintRendererInstance = renderer.GetComponent<MeshRenderer>();
        }
        else
        {
            Destroy(footprintPrefabInstance);
            footprintPrefabInstance = null;
        }
    }
    private void OnGadgetIDChanged(int gadgetID)
    {
        if (gadgetID == -1)
        {
            SetHeldGadget(playerItemController._gadgetItem, null);
            return;
        }
        
        var definition = actorManager.ActorTypes[gadgetID].Cast<GadgetDefinition>();
        SetHeldGadget(playerItemController._gadgetItem, definition);
    }

    public void SetHeldGadget(GadgetItem self, GadgetDefinition? gadgetDefinition)
    {
        Destroy(placeholderGadgetPrefabInstance);
        
        if (!gadgetDefinition)
            return;
         
        var gadgetDefinitionToPlace = self.GetGadgetDefinitionToPlace(gadgetDefinition);
        var prefab = gadgetDefinitionToPlace.prefab;
        var footprintTransform = footprintPrefabInstance!.transform;

        placeholderGadgetPrefabInstance = self.CopyPlaceholderGameObject(prefab, footprintTransform);
        placeholderGadgetPrefabInstance.SetActive(false);

        self.CopyMeshComponents(prefab);
        self.CopyGadgetComponents(prefab);
        self.CopySpecialComponents(prefab);

        placeholderGadgetPrefabInstance.SetActive(true);

        self.SetGadgetLayerRecursively(prefab.transform, prefab.SRGetComponent<Gadget>());

        placeholderGadgetPrefabInstance.transform.parent = footprintTransform;
        placeholderGadgetPrefabInstance.transform.localPosition = Vector3.zero;
        
        DontDestroyOnLoad(placeholderGadgetPrefabInstance);
    }
}