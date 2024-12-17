using System.Text;
using Microsoft.Data.SqlClient;
using Dapper;

namespace Afas.Bvr.Core.Repository;

/// <threadsafety static="true" instance="true"/>
public class MSSqlRepository : Repository
{
  private readonly string _connectionString;
  private readonly HashSet<string> _tablesCreated = new(StringComparer.OrdinalIgnoreCase);

  public MSSqlRepository(string connectionString)
  {
    _connectionString = connectionString;
  }

  public override async Task Add<TKey, TValue>(TValue newItem)
  {
    var tableName = typeof(TValue).Name;

    await CreateTableIfNotExists<TValue>();

    var sbInsert = new StringBuilder();
    var sbValues = new StringBuilder();
    sbInsert.Append($"INSERT INTO {tableName} (");

    foreach(var prop in typeof(TValue).GetProperties())
    {

      sbInsert.Append($"{prop.Name}, ");
      sbValues.Append($"@{prop.Name}, ");
    }

    sbInsert.Remove(sbInsert.Length - 2, 2);
    sbValues.Remove(sbValues.Length - 2, 2);

    sbInsert.Append(") VALUES (");
    sbInsert.Append(sbValues);
    sbInsert.Append(");");

    using var con = new SqlConnection(_connectionString);
    await con.ExecuteAsync(sbInsert.ToString(), newItem);
  }

  private async Task CreateTableIfNotExists<TValue>()
  {
    var tableName = typeof(TValue).Name;

    // check cache
    if(_tablesCreated.Contains(tableName))
    {
      return;
    }

    using var con = new SqlConnection(_connectionString);
    if(await con.ExecuteScalarAsync<int>($"select count(*) from sys.tables where name = '{tableName}';") > 0)
    {
      return;
    }

    var sb = new StringBuilder();
    sb.Append($"CREATE TABLE {tableName} (ID UNIQUEIDENTIFIER PRIMARY KEY, ");
    foreach(var prop in typeof(TValue).GetProperties())
    {
      // skip the id
      if(string.Equals(prop.Name, "ID", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      sb.Append($"{prop.Name} NVARCHAR(MAX), ");
    }

    sb.Remove(sb.Length - 2, 2);
    sb.Append(");");

    await con.ExecuteAsync(sb.ToString());

    // add to cache
    _tablesCreated.Add(tableName);
  }

  public override async Task Delete<TKey, TValue>(TKey id)
  {
    await CreateTableIfNotExists<TValue>();

    var tableName = typeof(TValue).Name;

    using var con = new SqlConnection(_connectionString);

    var parameters = new Dictionary<string, object> { { "Id", id } };
    await con.ExecuteAsync($"DELETE FROM {tableName} WHERE Id = @Id", parameters);
  }
}
