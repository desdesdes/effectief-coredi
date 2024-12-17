using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;

namespace Afas.Bvr.Core.Repository;

/// <threadsafety static="true" instance="true"/>
public class AzureStorageTableRepository : Repository
{
  readonly TableServiceClient _serviceClient;

  public AzureStorageTableRepository(IOptions<AzureStorageTableSettings> settings)
  {
    _serviceClient = new TableServiceClient(
      new Uri(settings.Value.Endpoint),
      new AzureSasCredential(settings.Value.SasSignature));
  }

  public override async Task Add<TValue>(TValue newItem)
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


      if(prop.PropertyType == typeof(DateOnly) || prop.PropertyType == typeof(DateOnly?))
      {
        var dateValue = (DateOnly?)prop.GetValue(newItem);

        if(dateValue.HasValue)
        {
          tableEntity.Add(prop.Name, dateValue.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        }
      }
      else
      {
        tableEntity.Add(prop.Name, prop.GetValue(newItem));
      }
    }

    await tableClient.AddEntityAsync(tableEntity);
  }

  public override async Task Delete<TValue>(Guid id)
  {
    var tableName = typeof(TValue).Name;

    await _serviceClient.CreateTableIfNotExistsAsync(tableName);

    var tableClient = _serviceClient.GetTableClient(tableName);

    _ = await tableClient.DeleteEntityAsync(id.ToString(), id.ToString());
  }

  public override async Task<TValue?> GetOrDefault<TValue>(Guid id) where TValue : class
  {
    var tableName = typeof(TValue).Name;

    await _serviceClient.CreateTableIfNotExistsAsync(tableName);

    var tableClient = _serviceClient.GetTableClient(tableName);

    var entity = await tableClient.GetEntityIfExistsAsync<TableEntity>(id.ToString(), id.ToString());

    if(!entity.HasValue)
    {
      return null;
    }

    var newItem = Activator.CreateInstance<TValue>();
    newItem.Id = id;

    var properties = typeof(TValue).GetProperties();
    foreach(var prop in properties)
    {
      // skip the id
      if(string.Equals(prop.Name, "ID", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      if(entity.Value!.TryGetValue(prop.Name, out var value))
      {
        if(prop.PropertyType == typeof(DateOnly) || prop.PropertyType == typeof(DateOnly?))
        {
          var dateValue = (DateTimeOffset?)value;

          if(dateValue.HasValue)
          {
            prop.SetValue(newItem, new DateOnly(dateValue.Value.Year, dateValue.Value.Month, dateValue.Value.Day));
          }
        }
        else
        {
          prop.SetValue(newItem, value);
        }
      }
    }

    return newItem;
  }
}
