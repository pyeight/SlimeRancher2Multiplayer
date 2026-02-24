using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace SR2MP.Shared.Utils;

public static class RecyclePool<T> where T : class, IRecyclable, new()
{
    private static readonly ConcurrentQueue<T> Pool = new();
    private static readonly Func<T> Create = CreateFactory();

    public static T Borrow()
    {
        var result = Pool.TryDequeue(out var item) ? item : Create();
        result.IsRecycled = false;
        return result;
    }

    public static void Return(T item)
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