using Afas.Bvr.Core.Repository;
using Afas.Bvr.Crm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleApp1;

internal class Program
{
  static async Task Main(string[] args)
  {
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
    builder.Services.AddAzureStorageTableRepository(builder.Configuration);
    builder.Services.AddSingleton<CrmMeters>();
    builder.Services.AddSingleton<PersonBC>();
    builder.Services.AddHttpClient();
    builder.Services.AddSingleton<PhonenumberChecker>();

    var host = builder.Build();

    var bc = host.Services.GetRequiredService<PersonBC>();

    var start = DateTime.UtcNow;
    while(DateTime.UtcNow - start < TimeSpan.FromSeconds(10))
    {
      var personId = Guid.NewGuid();
      Console.WriteLine("AddPerson");
      await bc.AddPerson(new Person { Id = personId, FirstName = "Bart", LastName = "Vries", Email = "bart.vries@afas.nl" });

      Console.WriteLine("GetPersonOrDefault");
      var retrieved = await bc.GetPersonOrDefault(personId);

      Console.WriteLine("DeletePerson");
      await bc.DeletePerson(personId);
    }

    Console.WriteLine("Done!");
  }
}
