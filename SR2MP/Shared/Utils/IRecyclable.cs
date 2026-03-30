namespace SR2MP.Shared.Utils;

/// <summary>
/// Defines a contract for objects that can be pooled and reused to minimize GC allocations.
/// </summary>
public interface IRecyclable
{
    /// <summary>
    /// Gets or sets a value indicating whether this instance is currently in the recycle pool.
    /// </summary>
    bool IsRecycled { get; set; }

    /// <summary>
    /// Clears the object's state and releases any held resources, preparing it for reuse.
    /// </summary>
    void Recycle();
}