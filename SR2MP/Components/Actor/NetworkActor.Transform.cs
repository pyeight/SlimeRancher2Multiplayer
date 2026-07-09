using SR2MP.Packets.Actor;
using SR2MP.Shared.Utils;

namespace SR2MP.Components.Actor;

internal sealed partial class NetworkActor
{
    public Vector3 previousPosition;
    public Vector3 nextPosition;
    private Vector3 lastSentPosition;
    
    public Quaternion previousRotation;
    public Quaternion nextRotation;
    private Quaternion lastSentRotation;
    
    private Vector3 lastSentVelocity;
    
    public float interpolationStart;
    public float interpolationEnd;

    private const int ForceSendInterval = 10;
    private int skippedUpdates;
    private bool staggerInitialized;

    public void OnNetworkWorldUpdate(ActorUpdatePacket packet)
    {
        if (LocallyOwned || IsDestroyed)
            return;

        previousPosition   = transform.position;
        previousRotation   = transform.rotation;
        nextPosition       = packet.Position;
        nextRotation       = packet.Rotation;
        savedVelocity      = packet.Velocity;
        interpolationStart = UnityEngine.Time.unscaledTime;
        interpolationEnd   = interpolationStart + Timers.ActorTimer;
    }
    
    private void UpdateInterpolation()
    {
        if (LocallyOwned || IsDestroyed || interpolationEnd <= interpolationStart)
            return;

        var now = UnityEngine.Time.unscaledTime;

        if (now <= interpolationEnd)
        {
            var t = Mathf.InverseLerp(interpolationStart, interpolationEnd, now);

            transform.position = Vector3.Lerp(previousPosition, nextPosition, t);
            transform.rotation = Quaternion.Lerp(previousRotation, nextRotation, t);
        }
        else
        {
            transform.position = nextPosition;
            transform.rotation = nextRotation;
        }

        if (rigidbody)
            rigidbody.velocity = savedVelocity;
    }

    private void SendWorldUpdate()
    {
        var currentPosition = transform.position;
        var currentRotation = transform.rotation;
        var currentVelocity = rigidbody ? rigidbody.velocity : Vector3.zero;

        if (!staggerInitialized)
        {
            var id = ActorId;

            if (id.Value != 0)
            {
                skippedUpdates = (int)(id.Value % ForceSendInterval);
                staggerInitialized = true;
            }
        }

        var changed = currentPosition != lastSentPosition
                   || currentRotation != lastSentRotation
                   || currentVelocity != lastSentVelocity;

        if (!changed && ++skippedUpdates < ForceSendInterval) return;

        skippedUpdates   = 0;
        lastSentPosition = currentPosition;
        lastSentRotation = currentRotation;
        lastSentVelocity = currentVelocity;

        previousPosition = currentPosition;
        previousRotation = currentRotation;
        nextPosition     = currentPosition;
        nextRotation     = currentRotation;

        var actorId = ActorId;

        if (actorId.Value == 0)
            return;

        Main.SendToAllOrServer(new ActorUpdatePacket
        {
            ActorId  = actorId,
            Position = currentPosition,
            Rotation = currentRotation,
            Velocity = currentVelocity
        });
    }
}