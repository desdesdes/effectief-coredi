using Azure;
using Azure.Data.Tables;

namespace Afas.Bvr.Core.Repository;

public class AzureStorageTableRepository : Repository
{
  readonly TableServiceClient _serviceClient;

  public AzureStorageTableRepository(string endpoint, string sasSignature)
  {
    _serviceClient = new TableServiceClient(
      new Uri(endpoint),
      new AzureSasCredential(sasSignature));
  }

  public override async Task Add<TKey, TValue>(TValue newItem)
  {
    var tableName = typeof(TValue).Name;

    await _serviceClient.CreateTableIfNotExistsAsync(tableName);

    var tableClient = _serviceClient.GetTableClient(tableName);

    var tableEntity = new TableEntity(newItem.Id.ToString(), newItem.Id.ToString());

    var properties = typeof(TValue).GetProperties();
    foreach(var prop in properties)
    {
      // skip the id
      if(string.Equals(prop.Name, "ID", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      tableEntity.Add(prop.Name, prop.GetValue(newItem));
    } 

    await tableClient.AddEntityAsync(tableEntity);
  }

  public override async Task Delete<TKey, TValue>(TKey id)
  {
    var tableName = typeof(TValue).Name;

    await _serviceClient.CreateTableIfNotExistsAsync(tableName);

    var tableClient = _serviceClient.GetTableClient(tableName);

    _ = await tableClient.DeleteEntityAsync(id.ToString(), id.ToString());
  }
}
