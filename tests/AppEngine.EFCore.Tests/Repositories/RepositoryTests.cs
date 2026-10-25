using AppEngine.EFCore.Repositories;
using AppEngine.EFCore.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AppEngine.EFCore.Tests.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly TestDatabase db = new(withInterceptors: false);

    public void Dispose() => db.Dispose();

    private static Repository<Tag, int, TestDbContext> Repo(TestDbContext context) => new(context);

    private async Task SeedAsync(int count)
    {
        await using var context = db.CreateContext();
        context.Tags.AddRange(Enumerable.Range(1, count).Select(i => new Tag { Label = $"tag-{i}" }));
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_PersistsImmediately()
    {
        await using (var context = db.CreateContext())
        {
            await Repo(context).AddAsync(new Tag { Label = "a" });
        }

        await using var verify = db.CreateContext();
        Assert.Equal(1, await verify.Tags.CountAsync());
    }

    [Fact]
    public async Task AddRangeAsync_PersistsAll()
    {
        await using (var context = db.CreateContext())
        {
            await Repo(context).AddRangeAsync([new Tag { Label = "a" }, new Tag { Label = "b" }]);
        }

        await using var verify = db.CreateContext();
        Assert.Equal(2, await verify.Tags.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ExistingAndMissing()
    {
        await SeedAsync(2);
        await using var context = db.CreateContext();
        var repo = Repo(context);

        Assert.Equal("tag-1", (await repo.GetByIdAsync(1))!.Label);
        Assert.Null(await repo.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUntracked()
    {
        await SeedAsync(3);
        await using var context = db.CreateContext();

        var items = await Repo(context).GetAllAsync();

        Assert.Equal(3, items.Count);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsRequestedPageAndMetadata()
    {
        await SeedAsync(25);
        await using var context = db.CreateContext();

        var page = await Repo(context).GetPagedAsync(2, 10);

        Assert.Equal(10, page.Items.Count);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task GetPagedAsync_LastPageIsPartial()
    {
        await SeedAsync(25);
        await using var context = db.CreateContext();

        var page = await Repo(context).GetPagedAsync(3, 10);

        Assert.Equal(5, page.Items.Count);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public async Task GetPagedAsync_BeyondLastPage_IsEmpty()
    {
        await SeedAsync(5);
        await using var context = db.CreateContext();

        var page = await Repo(context).GetPagedAsync(10, 10);

        Assert.Empty(page.Items);
        Assert.Equal(5, page.TotalCount);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    public async Task GetPagedAsync_InvalidArguments_Throw(int pageNumber, int pageSize)
    {
        await using var context = db.CreateContext();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Repo(context).GetPagedAsync(pageNumber, pageSize));
    }

    [Fact]
    public async Task ExistsAsync_And_CountAsync()
    {
        await SeedAsync(2);
        await using var context = db.CreateContext();
        var repo = Repo(context);

        Assert.True(await repo.ExistsAsync(1));
        Assert.False(await repo.ExistsAsync(42));
        Assert.Equal(2, await repo.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        await SeedAsync(1);
        await using (var context = db.CreateContext())
        {
            var repo = Repo(context);
            var tag = (await repo.GetByIdAsync(1))!;
            tag.Label = "changed";
            await repo.UpdateAsync(tag);
        }

        await using var verify = db.CreateContext();
        Assert.Equal("changed", (await verify.Tags.FindAsync(1))!.Label);
    }

    [Fact]
    public async Task UpdateRangeAsync_PersistsChanges()
    {
        await SeedAsync(2);
        await using (var context = db.CreateContext())
        {
            var tags = await context.Tags.ToListAsync();
            tags.ForEach(t => t.Label += "-x");
            await Repo(context).UpdateRangeAsync(tags);
        }

        await using var verify = db.CreateContext();
        Assert.All(await verify.Tags.ToListAsync(), t => Assert.EndsWith("-x", t.Label));
    }

    [Fact]
    public async Task DeleteAsync_And_DeleteRangeAsync_RemoveRows()
    {
        await SeedAsync(4);
        await using (var context = db.CreateContext())
        {
            var repo = Repo(context);
            await repo.DeleteAsync((await repo.GetByIdAsync(1))!);
            await repo.DeleteRangeAsync(await context.Tags.Where(t => t.Id > 2).ToListAsync());
        }

        await using var verify = db.CreateContext();
        Assert.Equal([2], await verify.Tags.Select(t => t.Id).ToListAsync());
    }

    [Fact]
    public async Task DeleteByIdAsync_ExistingRemoves_MissingIsNoOp()
    {
        await SeedAsync(1);
        await using (var context = db.CreateContext())
        {
            var repo = Repo(context);
            await repo.DeleteByIdAsync(999);
            await repo.DeleteByIdAsync(1);
        }

        await using var verify = db.CreateContext();
        Assert.Equal(0, await verify.Tags.CountAsync());
    }

    [Fact]
    public async Task SyncMethods_OnlyTrackChanges_UntilSaved()
    {
        await using var context = db.CreateContext();
        var repo = Repo(context);

        repo.Add(new Tag { Label = "a" });
        repo.AddRange([new Tag { Label = "b" }]);

        await using (var other = db.CreateContext())
        {
            Assert.Equal(0, await other.Tags.CountAsync());
        }

        await context.SaveChangesAsync();
        await using var verify = db.CreateContext();
        Assert.Equal(2, await verify.Tags.CountAsync());
    }

    [Fact]
    public async Task SyncUpdateAndDelete_MarkStates()
    {
        await SeedAsync(2);
        await using var context = db.CreateContext();
        var repo = Repo(context);
        var all = await context.Tags.ToListAsync();

        repo.Update(all[0]);
        repo.Delete(all[0]);
        repo.UpdateRange([all[1]]);
        repo.DeleteRange([all[1]]);

        Assert.All(all, t => Assert.Equal(EntityState.Deleted, context.Entry(t).State));
    }

    [Fact]
    public async Task Current_AsyncWrite_AlsoSavesPendingChangesAddedWithSyncMethods()
    {
        // Documents current behavior: the repository saves the whole DbContext on every async write,
        // so IUnitOfWork cannot batch work performed through the repository.
        await using (var context = db.CreateContext())
        {
            var repo = Repo(context);
            repo.Add(new Tag { Label = "pending" });
            await repo.AddAsync(new Tag { Label = "second" });
        }

        await using var verify = db.CreateContext();
        Assert.Equal(2, await verify.Tags.CountAsync());
    }

    [Theory]
    [InlineData("'; DROP TABLE Tags; --")]
    [InlineData("\" OR \"1\"=\"1")]
    [InlineData("<script>alert(1)</script>")]
    public async Task MaliciousStrings_AreStoredAsDataAndTableSurvives(string payload)
    {
        await using (var context = db.CreateContext())
        {
            await Repo(context).AddAsync(new Tag { Label = payload });
        }

        await using var verify = db.CreateContext();
        var tag = await verify.Tags.SingleAsync();
        Assert.Equal(payload, tag.Label);
    }
}
