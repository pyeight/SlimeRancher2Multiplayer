using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace SR2MP.Shared.Utils;

/// <summary>
/// Provides a thread-safe object pool for reusable items that implement <see cref="IRecyclable"/>.
/// </summary>
/// <typeparam name="T">The type of the object to pool. Must be a reference type, implement <see cref="IRecyclable"/>, and have a parameterless constructor.</typeparam>
public static class RecyclePool<T> where T : class, IRecyclable, new()
{
    private static readonly ConcurrentQueue<T> Pool = new();
    private static readonly Func<T> Create = CreateFactory();

    /// <summary>
    /// Retrieves an instance from the pool. If the pool is empty, a new instance is created.
    /// </summary>
    /// <returns>A valid, un-recycled instance of <typeparamref name="T"/>.</returns>
    public static T Borrow()
    {
        var result = Pool.TryDequeue(out var item) ? item : Create();
        result.IsRecycled = false;
        return result;
    }

    /// <summary>
    /// Returns an instance to the pool, resetting its state and marking it as recycled.
    /// </summary>
    /// <param name="item">The instance to return to the pool. If null, the operation is ignored.</param>
    /// <exception cref="InvalidOperationException">Thrown if the item is already marked as recycled.</exception>
    public static void Return(T? item)
    {
        if (item == null)
            return;

        if (item.IsRecycled)
            throw new InvalidOperationException("Item is already recycled!");

        item.Recycle();
        item.IsRecycled = true;
        Pool.Enqueue(item);
    }

    private static Func<T> CreateFactory()
    {
        var newExp = Expression.New(typeof(T));
        var lambda = Expression.Lambda<Func<T>>(newExp);
        return lambda.Compile();
    }
}