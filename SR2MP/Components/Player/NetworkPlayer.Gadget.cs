using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using Il2CppMonomiPark.SlimeRancher.Util.Extensions;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.Player;

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

    private void AwakeGadgetMode()
    {
        playerItemController = SceneContext.Instance.Player.GetComponent<PlayerItemController>();
        onNetworkGadgetModeChanged += OnGadgetModeChanged;
    }

    private void OnGadgetIDChanged(int gadgetID)
    {
        var definition = actorManager.ActorTypes[gadgetID].Cast<GadgetDefinition>();
        SetHeldGadget(playerItemController._gadgetItem, definition);
    }

    private void UpdateInterpolation()
    {
        if (!footprintPrefabInstance)
            return;

        if (interpolationEnd <= interpolationStart)
            return;

        var t = Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime);
        t = Mathf.Clamp01(t);

        footprintPrefabInstance!.transform.position = Vector3.Lerp(prevGadgetPosition, nextGadgetPosition, t);
        footprintPrefabInstance!.transform.rotation = Quaternion.Slerp(prevGadgetRotation, nextGadgetRotation, t);
    }

    private void UpdateGadgetMode()
    {
        if (IsLocal)
        {
            UpdateLocalGadgetMode();
            return;
        }

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
            UpdateInterpolation();
        }
    }

    private void UpdateLocalGadgetMode()
    {
        footprintPrefabInstance = playerItemController._gadgetItem._gadgetFootprintInstance;
        if (!footprintPrefabInstance)
            return;

        var packet = new PlayerGadgetUpdatePacket
        {
            Enabled = InGadgetMode,
            PlayerId = ID,
            Position = footprintPrefabInstance.transform.position,
            Rotation = footprintPrefabInstance.transform.localEulerAngles,
        };

        Main.SendToAllOrServer(packet);
    }

    public void OnGadgetPositionReceived(Vector3 newPosition, Vector3 newRotation)
    {
        prevGadgetPosition = footprintPrefabInstance != null
            ? footprintPrefabInstance.transform.position
            : newPosition;

        prevGadgetRotation = footprintPrefabInstance != null
            ? footprintPrefabInstance.transform.rotation
            : Quaternion.Euler(newRotation);

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
            footprintPrefabInstance.transform.rotation = nextGadgetRotation;
            prevGadgetPosition = nextGadgetPosition;
            prevGadgetRotation = nextGadgetRotation;
            Instantiate(footprintRendererPrefab, footprintPrefabInstance.transform, false);
        }
        else
        {
            Destroy(footprintPrefabInstance);
            footprintPrefabInstance = null;
        }
    }

    public void SetHeldGadget(GadgetItem self, GadgetDefinition gadgetDefinition)
    {
        var gadgetDefinitionToPlace = self.GetGadgetDefinitionToPlace(gadgetDefinition);
        var prefab = gadgetDefinitionToPlace.prefab;
        var footprintTransform = self._gadgetFootprintInstance.transform;

        var placeholderObject = self.CopyPlaceholderGameObject(prefab, footprintTransform);
        placeholderObject.SetActive(false);

        self.CopyMeshComponents(prefab);
        self.CopyGadgetComponents(prefab);
        self.CopySpecialComponents(prefab);

        placeholderObject.SetActive(true);

        self.SetGadgetLayerRecursively(prefab.transform, prefab.SRGetComponent<Gadget>());
    }
}