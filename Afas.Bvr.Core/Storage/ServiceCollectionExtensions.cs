using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Afas.Bvr.Core.Storage;

public static class ServiceCollectionExtensions
{
  extension(IServiceCollection services)
  {
    public IServiceCollection AddAzureStorageTableRepository(IConfiguration namedConfigurationSection)
    {
      services.Configure<AzureStorageTableOptions>(namedConfigurationSection);
      services.AddSingleton<Repository, AzureStorageTableRepository>();
      return services;
    }
  }
}
