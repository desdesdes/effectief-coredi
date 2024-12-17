using Dapper;

namespace Afas.Bvr.Core.Repository;

/// <threadsafety static="true" instance="true"/>
public abstract class Repository
{
  public abstract Task Add<TValue>(TValue newItem) where TValue: RepositoryObjectWithGuidId;
  public abstract Task Delete<TValue>(Guid id) where TValue : RepositoryObjectWithGuidId;
  public abstract Task<TValue?> GetOrDefault<TValue>(Guid id) where TValue : RepositoryObjectWithGuidId;
}
