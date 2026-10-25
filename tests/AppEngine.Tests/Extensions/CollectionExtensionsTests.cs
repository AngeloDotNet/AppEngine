using System.Runtime.CompilerServices;
using AppEngine.Extensions;
using AppCollection = AppEngine.Extensions.CollectionExtensions;

namespace AppEngine.Tests.Extensions;

public class CollectionExtensionsTests
{
    private static async IAsyncEnumerable<int> AsyncRange(int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return i;
        }
    }

    [Fact]
    public void DistinctBy_KeepsFirstElementPerKey_InOrder()
    {
        var result = AppCollection.DistinctBy(new[] { "apple", "avocado", "banana", "blueberry", "cherry" }, s => s[0]);

        Assert.Equal(["apple", "banana", "cherry"], result);
    }

    [Fact]
    public void DistinctBy_UsesComparer()
    {
        var result = AppCollection.DistinctBy(new[] { "a", "A", "b" }, s => s, StringComparer.OrdinalIgnoreCase);

        Assert.Equal(["a", "b"], result);
    }

    [Fact]
    public void Chunk_SplitsInOrder_LastChunkPartial()
    {
        var result = AppCollection.Chunk(Enumerable.Range(1, 5), 2).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal([1, 2], result[0]);
        Assert.Equal([3, 4], result[1]);
        Assert.Equal([5], result[2]);
    }

    [Fact]
    public void Chunk_EmptySource_ReturnsNoChunks() =>
        Assert.Empty(AppCollection.Chunk(Array.Empty<int>(), 3));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Chunk_InvalidSize_Throws(int size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => AppCollection.Chunk(new[] { 1 }, size));

    [Fact]
    public void ForEach_InvokesActionAndReturnsSource()
    {
        var seen = new List<int>();
        int[] source = [1, 2, 3];

        var returned = source.ForEach(seen.Add);

        Assert.Equal([1, 2, 3], seen);
        Assert.Same(source, returned);
    }

    [Fact]
    public async Task ForEachAsync_RunsSequentially()
    {
        var seen = new List<int>();

        await new[] { 1, 2, 3 }.ForEachAsync(async i =>
        {
            await Task.Yield();
            seen.Add(i);
        });

        Assert.Equal([1, 2, 3], seen);
    }

    [Fact]
    public async Task ForEachAsync_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new[] { 1 }.ForEachAsync(_ => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task SelectAsync_ProjectsInOrder()
    {
        var result = await new[] { 1, 2, 3 }.SelectAsync(async i =>
        {
            await Task.Yield();
            return i * 10;
        });

        Assert.Equal([10, 20, 30], result);
    }

    [Fact]
    public async Task SelectAsync_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new[] { 1 }.SelectAsync(i => Task.FromResult(i), cts.Token));
    }

    [Fact]
    public void IsEmpty_IsNotEmpty()
    {
        Assert.True(Array.Empty<int>().IsEmpty());
        Assert.False(Array.Empty<int>().IsNotEmpty());
        Assert.False(new[] { 1 }.IsEmpty());
        Assert.True(new[] { 1 }.IsNotEmpty());
    }

    [Fact]
    public void WhereIf_Enumerable_FiltersOnlyWhenConditionTrue()
    {
        int[] source = [1, 2, 3, 4];

        Assert.Equal([2, 4], source.WhereIf(true, i => i % 2 == 0));
        Assert.Equal(source, source.WhereIf(false, i => i % 2 == 0));
    }

    [Fact]
    public void WhereIf_Queryable_FiltersOnlyWhenConditionTrue()
    {
        var source = new[] { 1, 2, 3, 4 }.AsQueryable();

        Assert.Equal([2, 4], source.WhereIf(true, i => i % 2 == 0));
        Assert.Equal(4, source.WhereIf(false, i => i % 2 == 0).Count());
    }

    [Fact]
    public void EmptyIfNull_Enumerable()
    {
        IEnumerable<int>? nothing = null;
        int[] values = [1];

        Assert.Empty(nothing.EmptyIfNull());
        Assert.Same(values, values.EmptyIfNull());
    }

    [Fact]
    public void EmptyIfNull_Queryable()
    {
        IQueryable<int>? nothing = null;
        var values = new[] { 1 }.AsQueryable();

        Assert.Empty(nothing.EmptyIfNull());
        Assert.Same(values, values.EmptyIfNull());
    }

    [Fact]
    public async Task ToListAsync_MaterializesAsyncSequence()
    {
        var result = await AppCollection.ToListAsync(AsyncRange(3));

        Assert.Equal([0, 1, 2], result);
    }

    [Fact]
    public async Task ToListAsync_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await AppCollection.ToListAsync(AsyncRange(3), cts.Token));
    }

    [Fact]
    public void Remove_ByPredicate_RemovesAllMatches_List()
    {
        var list = new List<int> { 1, 2, 3, 4, 5, 6 };

        AppCollection.Remove(list, i => i % 2 == 0);

        Assert.Equal([1, 3, 5], list);
    }

    [Fact]
    public void Remove_ByPredicate_WorksOnHashSet()
    {
        ICollection<int> set = new HashSet<int> { 1, 2, 3, 4 };

        AppCollection.Remove(set, i => i > 2);

        Assert.Equal([1, 2], set.Order());
    }

    [Fact]
    public void Remove_ByPredicate_NoMatch_LeavesCollection()
    {
        var list = new List<int> { 1, 2 };

        AppCollection.Remove(list, i => i > 10);

        Assert.Equal([1, 2], list);
    }

    [Fact]
    public void NullOrEmptyHelpers()
    {
        IEnumerable<int>? nothing = null;

        Assert.True(nothing.IsNullOrEmpty());
        Assert.True(Array.Empty<int>().IsNullOrEmpty());
        Assert.False(new[] { 1 }.IsNullOrEmpty());

        Assert.False(nothing.IsNotNullOrEmpty());
        Assert.True(new[] { 1 }.IsNotNullOrEmpty());

        Assert.False(nothing.HasItems());
        Assert.True(new[] { 1 }.HasItems());
    }

    [Fact]
    public void GetCount_And_GetLongCount()
    {
        IEnumerable<int>? nothing = null;
        int[] values = [1, 2, 3, 4];

        Assert.Equal(0, nothing.GetCount());
        Assert.Equal(4, values.GetCount());
        Assert.Equal(2, values.GetCount(i => i > 2));

        Assert.Equal(0L, nothing.GetLongCount());
        Assert.Equal(4L, values.GetLongCount());
        Assert.Equal(3L, values.GetLongCount(i => i > 1));
    }
}
