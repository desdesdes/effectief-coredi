namespace Afas.Bvr.Core.Repository;

public abstract class RepositoryObject<T> where T : notnull
{
  public required virtual T Id { get; set; }
}
