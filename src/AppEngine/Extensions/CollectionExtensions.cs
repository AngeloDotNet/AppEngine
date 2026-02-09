using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace AppEngine.Extensions;

public static class CollectionExtensions
{
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null) => source.GroupBy(keySelector, comparer).Select(x => x.First());

    public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int chunkSize)
    {
        if (chunkSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than 0");
        }

        return source
            .Select((value, index) => new { Index = index, Value = value })
            .GroupBy(x => x.Index / chunkSize)
            .Select(g => g.Select(x => x.Value).ToArray());
    }

    public static IEnumerable<TSource> EmptyIfNull<TSource>(this IEnumerable<TSource>? source) => source ?? [];

    public static IQueryable<TSource> EmptyIfNull<TSource>(this IQueryable<TSource>? source) => source ?? Array.Empty<TSource>().AsQueryable();

    public static IEnumerable<TSource> ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
    {
        foreach (var item in source)
        {
            action(item);
        }

        return source;
    }

    public static async Task<IEnumerable<TSource>> ForEachAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, Task> action,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action.Invoke(item).ConfigureAwait(false);
        }

        return source;
    }

    public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource,
        Task<TResult>> selector, CancellationToken cancellationToken = default)
    {
        var result = new List<TResult>();

        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Add(await selector(item).ConfigureAwait(false));
        }

        return result;
    }

    public static async ValueTask<IEnumerable<TSource>> ToListAsync<TSource>(this IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken = default)
    {
        var list = new List<TSource>();

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }

    public static void Remove<TSource>(this ICollection<TSource> collection, Func<TSource, bool> predicate)
    {
        for (var i = collection.Count - 1; i >= 0; i--)
        {
            var element = collection.ElementAt(i);

            if (predicate(element))
            {
                collection.Remove(element);
            }
        }
    }

    public static bool IsEmpty<TSource>(this IEnumerable<TSource> source) => !source.Any();

    public static bool IsNotEmpty<TSource>(this IEnumerable<TSource> source) => source.Any();

    public static bool IsNullOrEmpty<TSource>([NotNullWhen(false)] this IEnumerable<TSource>? source) => !source?.Any() ?? true;

    public static bool IsNotNullOrEmpty<TSource>([NotNullWhen(true)] this IEnumerable<TSource>? source) => source?.Any() ?? false;

    public static bool HasItems<TSource>([NotNullWhen(true)] this IEnumerable<TSource>? source) => source.IsNotNullOrEmpty();

    public static int GetCount<TSource>(this IEnumerable<TSource>? source, Func<TSource, bool>? predicate = null)
        => (predicate is null ? source?.Count() : source?.Count(predicate)) ?? 0;

    public static long GetLongCount<TSource>(this IEnumerable<TSource>? source, Func<TSource, bool>? predicate = null)
        => (predicate is null ? source?.LongCount() : source?.LongCount(predicate)) ?? 0;

    public static IEnumerable<TSource> WhereIf<TSource>(this IEnumerable<TSource> source, bool condition, Func<TSource, bool> predicate)
        => condition ? source.Where(predicate) : source;

    public static IQueryable<TSource> WhereIf<TSource>(this IQueryable<TSource> source, bool condition, Expression<Func<TSource, bool>> predicate)
        => condition ? source.Where(predicate) : source;
}