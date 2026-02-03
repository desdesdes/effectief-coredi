using Afas.Bvr.Core.Storage;
using Microsoft.Extensions.Options;

namespace ConsoleApp1;

internal class AzureStorageTableConfigurator : IConfigureOptions<AzureStorageTableOptions>
{
  private readonly TimeProvider _timeProvider;

  public AzureStorageTableConfigurator(TimeProvider timeProvider)
  {
    _timeProvider = timeProvider;
  }

  public void Configure(AzureStorageTableOptions options)
  {
    if(_timeProvider.GetUtcNow() < new DateTime(2025, 12, 31))
    {
      options.SasSignature = string.Empty;
    }
  }
}
