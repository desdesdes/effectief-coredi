namespace Afas.Bvr.Core.Repository;

public abstract class RepositoryObject<TKey> where TKey : notnull
{
  public required virtual TKey Id { get; set; }
}
