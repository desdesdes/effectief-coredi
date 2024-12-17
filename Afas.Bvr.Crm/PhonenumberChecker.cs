using System.Net.Http.Json;

namespace Afas.Bvr.Crm;

public class PhonenumberChecker
{
  private readonly HttpClient _httpClient;

  public PhonenumberChecker(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public virtual async Task<bool> CheckPhoneNumber(string? phoneNumber)
  {
    return !string.IsNullOrEmpty(phoneNumber) &&
      await _httpClient.GetFromJsonAsync<bool>($"https://coredidemo.azurewebsites.net/phonenumbercheck.html?number={Uri.EscapeDataString(phoneNumber)}");
  }
}
