using Dapper;

namespace Afas.Bvr.Core.Repository;

/// <threadsafety static="true" instance="true"/>
public abstract class Repository
{
  public abstract Task Add<TValue>(TValue newItem) where TValue: RepositoryObjectWithGuidId;
  public abstract Task Delete<TValue>(Guid id) where TValue : RepositoryObjectWithGuidId;
  public abstract Task<TValue?> GetOrDefault<TValue>(Guid id) where TValue : RepositoryObjectWithGuidId;

  public static Repository CreateRepository(StorageSettings settings)
  {
    return settings.StorageType switch
    {
      StorageType.MsSql => new MSSqlRepository(settings.MsSqlConnectionString!),
      StorageType.AzureStorageTable => new AzureStorageTableRepository(settings.AzureStorageTableEndpoint!, settings.AzureStorageTableSasSignature!),
      _ => throw new ArgumentException("Invalid storage type"),
    };
  }
}
