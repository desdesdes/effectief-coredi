using System.Text.Json;
using Afas.Bvr.Core.Logging;
using Afas.Bvr.Core.Storage;
using Afas.Bvr.Crm;

namespace ConsoleApp1;

internal class Program
{
  static async Task Main(string[] args)
  {
    var settings = JsonSerializer.Deserialize<StorageSettings>(File.ReadAllText("appsettings.json"))!;
    var logger = new ConsoleLogger();

    var bc = new PersonBC(settings, logger);

    var personId = Guid.NewGuid();
    Console.WriteLine("AddPerson");
    await bc.AddPerson(new Person { Id=personId, FirstName="Bart", LastName="Vries", Email="bart.vries@afas.nl" });

    Console.WriteLine("GetPersonOrDefault");
    var retrieved = await bc.GetPersonOrDefault(personId);

    Console.WriteLine("DeletePerson");
    await bc.DeletePerson(personId);

    Console.WriteLine("Done!");
  }
}
