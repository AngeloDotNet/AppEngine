using AppEngine.EFCore.Interceptors;
using AppEngine.EFCore.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AppEngine.EFCore.Tests.Interceptors;

public class InterceptorTests : IDisposable
{
    private readonly TestDatabase db = new("alice");

    public void Dispose() => db.Dispose();

    [Fact]
    public void Constructors_NullService_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new AuditSaveChangesInterceptor(null!));
        Assert.Throws<ArgumentNullException>(() => new SoftDeleteInterceptor(null!));
    }

    // Audit

    [Fact]
    public async Task Audit_Added_SetsCreatedFields()
    {
        var before = DateTime.UtcNow;
        await using (var context = db.CreateContext())
        {
            context.Notes.Add(new AuditedNote { Text = "a" });
            await context.SaveChangesAsync();
        }

        await using var verify = db.CreateContext();
        var note = await verify.Notes.SingleAsync();
        Assert.Equal("alice", note.CreatedBy);
        Assert.InRange(note.CreatedAt, before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        Assert.Null(note.UpdatedAt);
        Assert.Null(note.UpdatedBy);
    }

    [Fact]
    public async Task Audit_Modified_SetsUpdatedFields_AndKeepsCreated()
    {
        await using (var context = db.CreateContext())
        {
            context.Notes.Add(new AuditedNote { Text = "a" });
            await context.SaveChangesAsync();
        }

        db.User.UserName = "bob";
        await using (var context = db.CreateContext())
        {
            var note = await context.Notes.SingleAsync();
            note.Text = "b";
            await context.SaveChangesAsync();
        }

        await using var verify = db.CreateContext();
        var saved = await verify.Notes.SingleAsync();
        Assert.Equal("alice", saved.CreatedBy);
        Assert.Equal("bob", saved.UpdatedBy);
        Assert.NotNull(saved.UpdatedAt);
    }

    [Fact]
    public async Task Audit_NoUser_FallsBackToSystem()
    {
        db.User.UserName = null;
        await using (var context = db.CreateContext())
        {
            context.Notes.Add(new AuditedNote { Text = "a" });
            await context.SaveChangesAsync();
        }

        await using var verify = db.CreateContext();
        Assert.Equal("System", (await verify.Notes.SingleAsync()).CreatedBy);
    }

    [Fact]
    public async Task Audit_NonAuditableEntity_IsIgnored()
    {
        await using var context = db.CreateContext();
        context.Tags.Add(new Tag { Label = "a" });

        Assert.Equal(1, await context.SaveChangesAsync());
    }

    [Fact]
    public async Task Audit_ClientSuppliedCreatedBy_IsOverwritten()
    {
        await using (var context = db.CreateContext())
        {
            context.Notes.Add(new AuditedNote { Text = "a", CreatedBy = "attacker", CreatedAt = new DateTime(2000, 1, 1) });
            await context.SaveChangesAsync();
        }

        await using var verify = db.CreateContext();
        var note = await verify.Notes.SingleAsync();
        Assert.Equal("alice", note.CreatedBy);
        Assert.True(note.CreatedAt.Year >= 2025);
    }

    [Fact]
    public void Current_Audit_SyncSaveChanges_DoesNotSetAuditFields()
    {
        // Documents current behavior: only SavingChangesAsync is overridden, so SaveChanges() skips auditing.
        using (var context = db.CreateContext())
        {
            context.Notes.Add(new AuditedNote { Text = "a" });
            context.SaveChanges();
        }

        using var verify = db.CreateContext();
        var note = verify.Notes.Single();
        Assert.Null(note.CreatedBy);
        Assert.Equal(default, note.CreatedAt);
    }

    // Soft delete

    [Fact]
    public async Task SoftDelete_Remove_BecomesFlaggedUpdate()
    {
        await using (var context = db.CreateContext())
        {
            context.Products.Add(new Product { Name = "p" });
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var product = await context.Products.SingleAsync();
            context.Products.Remove(product);
            await context.SaveChangesAsync();
        }

        await using var verify = db.CreateContext();
        var saved = await verify.Products.SingleAsync();
        Assert.True(saved.IsDeleted);
        Assert.Equal("alice", saved.DeletedBy);
        Assert.NotNull(saved.DeletedAt);
    }

    [Fact]
    public async Task Current_SoftDelete_DoesNotSetUpdatedFields_WhenAuditInterceptorIsRegisteredFirst()
    {
        // Documents current behavior: the audit interceptor runs before the soft delete one turns Deleted into Modified,
        // so UpdatedAt/UpdatedBy stay empty. The result depends on the interceptor registration order.
        await using (var context = db.CreateContext())
        {
            context.Products.Add(new Product { Name = "p" });
            await context.SaveChangesAsync();
            context.Products.Remove(await context.Products.SingleAsync());
            await context.SaveChangesAsync();
        }

        await using var verify = db.CreateContext();
        Assert.Null((await verify.Products.SingleAsync()).UpdatedAt);
    }

    [Fact]
    public async Task SoftDelete_NonSoftDeleteEntity_IsHardDeleted()
    {
        await using (var context = db.CreateContext())
        {
            context.Tags.Add(new Tag { Label = "a" });
            await context.SaveChangesAsync();
            context.Tags.Remove(await context.Tags.SingleAsync());
            await context.SaveChangesAsync();
        }

        await using var verify = db.CreateContext();
        Assert.Equal(0, await verify.Tags.CountAsync());
    }

    [Fact]
    public async Task Current_SoftDelete_DeletedRowsAreStillReturnedByQueries()
    {
        // Documents current behavior: no global query filter is registered by the library.
        await using var context = db.CreateContext();
        context.Products.Add(new Product { Name = "p" });
        await context.SaveChangesAsync();
        context.Products.Remove(await context.Products.SingleAsync());
        await context.SaveChangesAsync();

        Assert.Equal(1, await context.Products.CountAsync());
    }

    [Fact]
    public void Current_SoftDelete_SyncSaveChanges_HardDeletes()
    {
        // Documents current behavior: only SavingChangesAsync is overridden, so SaveChanges() really deletes the row.
        using (var context = db.CreateContext())
        {
            context.Products.Add(new Product { Name = "p" });
            context.SaveChanges();
            context.Products.Remove(context.Products.Single());
            context.SaveChanges();
        }

        using var verify = db.CreateContext();
        Assert.Equal(0, verify.Products.Count());
    }
}
