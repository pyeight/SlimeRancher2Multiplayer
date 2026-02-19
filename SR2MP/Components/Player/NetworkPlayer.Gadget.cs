using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Packets.Player;
using SR2MP.Shared.Utils;

namespace SR2MP.Components.Player;

public partial class NetworkPlayer
{
    public event Action<bool> onNetworkGadgetModeChanged;

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

    private void UpdateInterpolation(GameObject obj)
    {
        if (interpolationEnd <= interpolationStart)
            return;

        var t = Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime);
        t = Mathf.Clamp01(t);

        obj.transform.position = Vector3.Lerp(prevGadgetPosition, nextGadgetPosition, t);
        obj.transform.rotation = Quaternion.Slerp(prevGadgetRotation, nextGadgetRotation, t);
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

        if (footprintPrefabInstance)
        {
            UpdateInterpolation(footprintPrefabInstance!);
        }
    }

    private void UpdateLocalGadgetMode()
    {
        footprintPrefabInstance = playerItemController._gadgetItem._gadgetFootprintInstance;

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

        interpolationStart = UnityEngine.Time.unscaledTime;
        interpolationEnd = UnityEngine.Time.unscaledTime + Timers.PlayerTimer;
    }

    private void OnGadgetModeChanged(bool newMode)
    {
        if (newMode)
        {
            var footprintPrefab = playerItemController._gadgetItem._gadgetItemMetadata.GadgetFootprintPrefab;
            footprintPrefabInstance = Instantiate(footprintPrefab);
            DontDestroyOnLoad(footprintPrefabInstance);

            footprintPrefabInstance.transform.position = nextGadgetPosition;
            footprintPrefabInstance.transform.rotation = nextGadgetRotation;
            prevGadgetPosition = nextGadgetPosition;
            prevGadgetRotation = nextGadgetRotation;
        }
        else
        {
            Destroy(footprintPrefabInstance);
            footprintPrefabInstance = null;
        }
    }
}