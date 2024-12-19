using System.Text;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;

namespace Afas.Bvr.Core.Repository;

/// <threadsafety static="true" instance="true"/>
public class MSSqlRepository : Repository
{
  private class SqlDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
  {
    public override void SetValue(IDbDataParameter parameter, DateOnly date)
        => parameter.Value = date.ToDateTime(new TimeOnly(0, 0));

    public override DateOnly Parse(object value) => DateOnly.FromDateTime((DateTime)value);
  }

  private readonly string _connectionString;
  private readonly HashSet<string> _tablesCreated = new(StringComparer.OrdinalIgnoreCase);

  public MSSqlRepository(string connectionString)
  {
    // Add support for DateOnly and TimeOnly to Dapper
    SqlMapper.AddTypeHandler(new SqlDateOnlyTypeHandler());

    _connectionString = connectionString;
  }

  public override async Task Add<TValue>(TValue newItem)
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

      if(prop.PropertyType == typeof(DateOnly) || prop.PropertyType == typeof(DateOnly?))
      {
        sb.Append($"{prop.Name} DATE, ");
      }
      else
      {
        sb.Append($"{prop.Name} NVARCHAR(MAX), ");
      }
    }

    sb.Remove(sb.Length - 2, 2);
    sb.Append(");");

    await con.ExecuteAsync(sb.ToString());

    // add to cache
    _tablesCreated.Add(tableName);
  }

  public override async Task Delete<TValue>(Guid id)
  {
    await CreateTableIfNotExists<TValue>();

    var tableName = typeof(TValue).Name;

    using var con = new SqlConnection(_connectionString);

    var parameters = new Dictionary<string, object> { { "Id", id } };
    await con.ExecuteAsync($"DELETE FROM {tableName} WHERE Id = @Id", parameters);
  }

  public override async Task<TValue?> GetOrDefault<TValue>(Guid id) where TValue : class
  {
    await CreateTableIfNotExists<TValue>();

    var tableName = typeof(TValue).Name;

    using var con = new SqlConnection(_connectionString);
    var result = await con.QuerySingleOrDefaultAsync<TValue>($"SELECT * FROM {tableName} WHERE Id = @Id", new { Id = id });
    return result;
  }
}
