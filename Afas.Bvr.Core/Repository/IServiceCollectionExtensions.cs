using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Afas.Bvr.Core.Repository;

public static class IServiceCollectionExtensions
{
  public static IServiceCollection AddAzureStorageTableRepository(this IServiceCollection services, IConfiguration namedConfigurationSection)
  {
    services.Configure<AzureStorageTableSettings>(namedConfigurationSection);
    services.AddSingleton<Repository, AzureStorageTableRepository>();
    return services;
  }
}
