using AppEngine.EFCore.Entities.Interfaces;

namespace AppEngine.EFCore.Entities;

public abstract class BaseSoftDelete<TKey> : BaseAuditable<TKey>, ISoftDelete where TKey : IEquatable<TKey>
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}