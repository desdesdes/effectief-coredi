namespace Afas.Bvr.Core.Repository;

public abstract class RepositoryObjectWithGuidId
{
  public required virtual Guid Id { get; set; }
}
