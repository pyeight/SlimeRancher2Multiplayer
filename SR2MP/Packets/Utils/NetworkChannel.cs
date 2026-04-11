using JetBrains.Annotations;

namespace SR2MP.Packets.Utils;

/// <summary>
/// Network channels packets can be sent on
/// </summary>
[PublicAPI]
public enum NetworkChannel : byte
{
    /// <summary>
    /// Default channel, do not use this
    /// </summary>
    Default = 0,
    
    /// <summary>
    /// General purpose channel for important infrequent packets
    /// Connecting, mod sync (ack), resync, joins, leaves
    /// </summary>
    Important = 1,

    /// <summary>
    /// Player updates
    /// Position/rotation/animation and gadget-mode updates
    /// </summary>
    PlayerUpdate = 2,

    /// <summary>
    /// Critical actor related packets
    /// Spawning and destroying 
    /// </summary>
    ActorCritical = 3,
    
    /// <summary>
    /// Actor updates
    /// Position, rotation and state
    /// </summary>
    ActorUpdate = 4,

    /// <summary>
    /// Weather related packets
    /// Weather updates and events (lightning strikes, tornados, (vines)) and time
    /// </summary>
    Weather = 5,
    
    /// <summary>
    /// Landplot related packets
    /// Creation/destruction, updating (garden plants, inventories (silos, auto feeders, plort collectors))
    /// </summary>
    Landplots = 6,
    
    /// <summary>
    /// World state related packets
    /// Gordo slimes, switches, treasure pods, geysers, map, slimepedia, upgrades
    /// </summary>
    WorldState = 7,
    
    /// <summary>
    /// Ammo related packets
    /// Silos, plort collectors, auto feeders, refinery
    /// </summary>
    Ammo = 8,

    /// <summary>
    /// Economy related packets
    /// Currency, market prices
    /// </summary>
    Economy = 9,
    
    /// <summary>
    /// FX-related packets
    /// Particles and sounds (vac, footsteps)
    /// </summary>
    FX = 10,
    
    /// <summary>
    /// Chat messages
    /// </summary>
    Chat = 11,
    
    /// <summary>
    /// Packet acknowledgements for reliable packets
    /// </summary>
    Acknowledge = 12,
    
    /// <summary>
    /// API (use the other channels if you can)
    /// Useful for small things that should still be seperated
    /// </summary>
    API = 13
}