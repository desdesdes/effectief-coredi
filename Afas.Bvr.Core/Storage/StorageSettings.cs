namespace Afas.Bvr.Core.Storage;

public class StorageSettings
{
  public StorageType StorageType { get; set; }
  public string? MsSqlConnectionString { get; set; }
  public string? AzureStorageTableEndpoint { get; set; }
  public string? AzureStorageTableSasSignature { get; set; }
}
