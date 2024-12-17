namespace Afas.Bvr.Core.Repository;

/// <threadsafety static="true" instance="true"/>
public abstract class Repository
{
  public abstract Task Add<TKey, TValue>(TValue newItem) where TValue: RepositoryObject<TKey> where TKey : notnull;
  public abstract Task Delete<TKey, TValue>(TKey id) where TValue : RepositoryObject<TKey> where TKey : notnull;

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
