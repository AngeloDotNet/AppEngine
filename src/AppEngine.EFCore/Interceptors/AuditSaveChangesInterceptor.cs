using AppEngine.EFCore.Entities.Interfaces;
using AppEngine.EFCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AppEngine.EFCore.Interceptors;

public class AuditSaveChangesInterceptor(ICurrentUserService currentUserService) : SaveChangesInterceptor
{
    private readonly ICurrentUserService currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;

        if (dbContext is not null)
        {
            var utcNow = DateTime.UtcNow;
            var userName = currentUserService.UserName ?? "System";

            var entities = dbContext.ChangeTracker.Entries<IAuditable>();
            foreach (var entityEntry in entities)
            {
                if (entityEntry.State is EntityState.Added)
                {
                    entityEntry.Property(p => p.CreatedAt).CurrentValue = utcNow;
                    entityEntry.Property(p => p.CreatedBy).CurrentValue = userName;
                }

                if (entityEntry.State is EntityState.Modified)
                {
                    entityEntry.Property(p => p.UpdatedAt).CurrentValue = utcNow;
                    entityEntry.Property(p => p.UpdatedBy).CurrentValue = userName;
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}