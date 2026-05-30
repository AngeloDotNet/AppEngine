using AppEngine.EFCore.Entities.Interfaces;
using AppEngine.EFCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AppEngine.EFCore.Interceptors;

public class SoftDeleteInterceptor(ICurrentUserService currentUserService) : SaveChangesInterceptor
{
    private readonly ICurrentUserService currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var utcNow = DateTime.UtcNow;
        var userName = currentUserService.UserName ?? "System";

        var entries = context.ChangeTracker.Entries<ISoftDelete>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (var entry in entries)
        {
            if (entry.Entity is ISoftDelete && entry.State is EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Property(p => p.IsDeleted).CurrentValue = true;
                entry.Property(p => p.DeletedAt).CurrentValue = utcNow;
                entry.Property(p => p.DeletedBy).CurrentValue = userName;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}