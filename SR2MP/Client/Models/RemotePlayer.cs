using UnityEngine;

namespace SR2MP.Client.Models;

public class RemotePlayer
{
    public string PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
    public DateTime LastUpdate { get; set; }

    // Animation stuff
    public int AirborneState { get; set; }
    public bool Moving { get; set; }
    public float Yaw { get; set; }
    public float HorizontalMovement { get; set; }
    public float ForwardMovement { get; set; }
    public float HorizontalSpeed { get; set; }
    public float ForwardSpeed { get; set; }
    public bool Sprinting { get; set; }
    
    public float LookY { get; set; }
    public float LastLookY { get; set; }

    public RemotePlayer(string playerId)
    {
        PlayerId = playerId;
        LastUpdate = DateTime.UtcNow;
    }
}