using SR2MP.Packets.Player;

namespace SR2MP.Client.Models;

/// <summary>
/// Represents a remote player in a multiplayer environment, tracking their position, rotation, and animation states.
/// </summary>
public sealed class RemotePlayer
{
    /// <summary>
    /// The unique identifier for the remote player.
    /// </summary>
    public readonly string PlayerId;

    /// <summary>
    /// Gets the username of the player.
    /// </summary>
    public string Username { get; internal set; }

    /// <summary>
    /// Gets the current 3D world position of the player.
    /// </summary>
    public Vector3 Position { get; internal set; }

    /// <summary>
    /// Gets the current rotation of the player.
    /// </summary>
    public float Rotation { get; internal set; }
    
    /// <summary>
    /// Gets the current SceneGroup of the player.
    /// </summary>
    public int SceneGroup { get; internal set; }

    // Animation stuff

    /// <summary>
    /// Gets the current airborne state of the player.
    /// </summary>
    public int AirborneState { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the player is currently moving.
    /// </summary>
    public bool Moving { get; internal set; }

    /// <summary>
    /// Gets the yaw angle used for driving player animations.
    /// </summary>
    public float Yaw { get; internal set; }

    /// <summary>
    /// Gets the player's horizontal movement value.
    /// </summary>
    public float HorizontalMovement { get; internal set; }

    /// <summary>
    /// Gets the player's forward or backward movement value.
    /// </summary>
    public float ForwardMovement { get; internal set; }

    /// <summary>
    /// Gets the player's current horizontal movement speed.
    /// </summary>
    public float HorizontalSpeed { get; internal set; }

    /// <summary>
    /// Gets the player's current forward movement speed.
    /// </summary>
    public float ForwardSpeed { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the player is currently sprinting.
    /// </summary>
    public bool Sprinting { get; internal set; }

    /// <summary>
    /// Gets the current vertical look angle (pitch) of the player's view.
    /// </summary>
    public float LookY { get; internal set; }

    /// <summary>
    /// Gets the previous frame's vertical look angle.
    /// </summary>
    public float LastLookY { get; internal set; }
    
    public bool OnlineGadgetMode { get; internal set; }
    public GadgetPlacementValidity OnlineGadgetValidity { get; internal set; }
    public int OnlineGadgetID { get; internal set; }
    public Vector3 OnlineGadgetPosition { get; internal set; }
    public Quaternion OnlineGadgetRotation { get; internal set; }
    public Quaternion OnlineGadgetLocalRotation { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemotePlayer"/> class.
    /// </summary>
    /// <param name="playerId">The unique identifier to assign to this player.</param>
    public RemotePlayer(string playerId) => PlayerId = playerId;
}