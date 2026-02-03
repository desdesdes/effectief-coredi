using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Afas.Bvr.Crm;

public class CrmDependencies
{
  private readonly HttpClient _httpClient;

  public CrmDependencies(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public virtual async Task ValidateExternalUserNameAsync(string? firstName, string? lastName)
  {
    var response = await _httpClient.GetFromJsonAsync<JsonObject>($"https://jsonplaceholder.typicode.com/users/1");
    if(string.Equals(response?["name"]?.GetValue<string>(), $"{firstName} {lastName}", StringComparison.Ordinal))
    {
      throw new Exception($"Person with name '{firstName} {lastName}' already exists in external system.");
    }
  }
}
