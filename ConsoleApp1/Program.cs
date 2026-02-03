using Afas.Bvr.Core.Storage;
using Afas.Bvr.Crm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleApp1;

internal class Program
{
  static async Task Main(string[] args)
  {
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddAzureStorageTableRepository(builder.Configuration);
    builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
    builder.Services.AddHttpClient();

    builder.Services.ConfigureOptions<AzureStorageTableConfigurator>();

    using var host = builder.Build();
    await host.StartAsync();

    var crmDependencies = ActivatorUtilities.CreateInstance<CrmDependencies>(host.Services);
    var crmMeters = ActivatorUtilities.CreateInstance<CrmMeters>(host.Services);
    var bc = ActivatorUtilities.CreateInstance<PersonBC>(host.Services, crmDependencies, crmMeters);

    Guid personId = Guid.NewGuid();
    for (int i = 0; i < 50; i++)
    {
      Console.WriteLine("AddPerson");
      await bc.AddPerson(new Person { Id = personId, FirstName = "Bart", LastName = "Vries", Email = "bart.vries@afas.nl" });
      personId = Guid.NewGuid();
      await Task.Delay(1000);
    }

    Console.WriteLine("GetPersonOrDefault");
    var retrieved = await bc.GetPersonOrDefault(personId);

    Console.WriteLine("DeletePerson");
    await bc.DeletePerson(personId);

    Console.WriteLine("Done!");

   await host.StopAsync();
  }
}
