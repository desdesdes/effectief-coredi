namespace Afas.Bvr.Core.Repository;

public abstract class Repository
{
  public abstract Task Add<TKey, TValue>(TValue newItem) where TValue: RepositoryObject<TKey> where TKey : notnull;
  public abstract Task Delete<TKey, TValue>(TKey id) where TValue : RepositoryObject<TKey> where TKey : notnull;
}
