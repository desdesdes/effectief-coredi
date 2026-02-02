namespace Afas.Bvr.Core.Storage;

public abstract class RepositoryObjectWithGuidId
{
  public required virtual Guid Id { get; set; }
}
