namespace Afas.Bvr.Core.Repository.Tests;

[TestFixture()]
[Category("Dep:MSSql")] // Let op, dit helpt om snel tests te filteren, omdat deze een sql dependency hebben
public class MSSqlRepositoryTests : RepositoryTests
{
  private readonly string _connectionString = "Server=.\\profitsqldev;Database=codedidemo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

  public override Repository CreateRepository()
  {
    return new MSSqlRepository(_connectionString);
  }
}
