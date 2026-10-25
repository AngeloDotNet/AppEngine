using AppEngine.EFCore.Tests.TestSupport;
using AppEngine.EFCore.UoW;
using Microsoft.EntityFrameworkCore;

namespace AppEngine.EFCore.Tests.UoW;

public class UnitOfWorkTests : IDisposable
{
    private readonly TestDatabase db = new(withInterceptors: false);

    public void Dispose() => db.Dispose();

    [Fact]
    public void Constructor_NullContext_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork<TestDbContext>(null!));

    [Fact]
    public async Task SaveChangesAsync_ReturnsAffectedRows()
    {
        await using var context = db.CreateContext();
        var uow = new UnitOfWork<TestDbContext>(context);
        context.Tags.AddRange(new Tag { Label = "a" }, new Tag { Label = "b" });

        Assert.Equal(2, await uow.SaveChangesAsync());
    }

    [Fact]
    public async Task Commit_PersistsChanges()
    {
        await using (var context = db.CreateContext())
        {
            var uow = new UnitOfWork<TestDbContext>(context);
            await uow.BeginTransactionAsync();
            context.Tags.Add(new Tag { Label = "a" });
            await uow.SaveChangesAsync();
            await uow.CommitTransactionAsync();
        }

        await using var verify = db.CreateContext();
        Assert.Equal(1, await verify.Tags.CountAsync());
    }

    [Fact]
    public async Task Rollback_DiscardsChanges()
    {
        await using (var context = db.CreateContext())
        {
            var uow = new UnitOfWork<TestDbContext>(context);
            await uow.BeginTransactionAsync();
            context.Tags.Add(new Tag { Label = "a" });
            await uow.SaveChangesAsync();
            await uow.RollbackTransactionAsync();
        }

        await using var verify = db.CreateContext();
        Assert.Equal(0, await verify.Tags.CountAsync());
    }

    [Fact]
    public async Task BeginTransaction_Twice_DoesNotThrowAndReusesTransaction()
    {
        await using var context = db.CreateContext();
        var uow = new UnitOfWork<TestDbContext>(context);

        await uow.BeginTransactionAsync();
        await uow.BeginTransactionAsync();
        await uow.CommitTransactionAsync();

        Assert.Null(context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task CommitAndRollback_WithoutTransaction_AreNoOps()
    {
        await using var context = db.CreateContext();
        var uow = new UnitOfWork<TestDbContext>(context);

        await uow.CommitTransactionAsync();
        await uow.RollbackTransactionAsync();
    }

    [Fact]
    public async Task CanBeginNewTransaction_AfterCommit()
    {
        await using var context = db.CreateContext();
        var uow = new UnitOfWork<TestDbContext>(context);

        await uow.BeginTransactionAsync();
        await uow.CommitTransactionAsync();
        await uow.BeginTransactionAsync();

        Assert.NotNull(context.Database.CurrentTransaction);
        await uow.RollbackTransactionAsync();
    }
}
