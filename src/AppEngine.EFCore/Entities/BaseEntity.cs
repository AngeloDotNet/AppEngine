using AppEngine.EFCore.Entities.Interfaces;

namespace AppEngine.EFCore.Entities;

public abstract class BaseEntity<TKey> : IEntity<TKey> where TKey : IEquatable<TKey>
{
    public TKey Id { get; set; } = default!;
}