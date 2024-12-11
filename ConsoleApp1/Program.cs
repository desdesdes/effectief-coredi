using Afas.Bvr.Core.Repository;
using Afas.Bvr.Crm;

namespace ConsoleApp1;

internal class Program
{
  static async Task Main(string[] args)
  {
    Console.WriteLine("Hello, World!");

    var repo = new AzureStorageTableRepository(@"https://codedidemo.table.core.windows.net/", @"sv=2022-11-02&ss=t&srt=sco&sp=rwdlacu&se=2028-12-11T23:55:39Z&st=2024-12-11T15:55:39Z&spr=https&sig=e684bQmmbwMXysmGBlbIlA4h365DFVDlJa1nVVeINOk%3D");
    //var repo = new MsSqlRepository(@"Server=.\profitsqldev;Database=codedidemo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;");

    var bc = new PersonBC(repo);

    var personId = Guid.NewGuid();
    await bc.AddPerson(new Person { Id=personId, FirstName="Bart", LastName="Vries", Email="bart.vries@afas.nl" });
    await bc.DeletePerson(personId);
  }
}
