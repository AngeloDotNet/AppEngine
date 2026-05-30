namespace AppEngine.EFCore.Entities.Interfaces;

public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; set; }
}