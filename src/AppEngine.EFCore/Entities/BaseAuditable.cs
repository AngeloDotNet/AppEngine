using AppEngine.EFCore.Entities.Interfaces;

namespace AppEngine.EFCore.Entities;

public abstract class BaseAuditable<TKey> : BaseEntity<TKey>, IAuditable where TKey : IEquatable<TKey>
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}