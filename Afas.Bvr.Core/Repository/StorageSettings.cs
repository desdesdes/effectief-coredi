namespace Afas.Bvr.Core.Repository;

public class StorageSettings
{
  public StorageType StorageType { get; set; }
  public string? MsSqlConnectionString { get; set; }
  public string? AzureStorageTableEndpoint { get; set; }
  public string? AzureStorageTableSasSignature { get; set; }
}
