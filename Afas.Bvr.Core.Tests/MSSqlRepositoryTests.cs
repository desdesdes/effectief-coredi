using System;
using System.Collections.Generic;
using System.Text;
using Afas.Bvr.Core.Storage;
using Microsoft.Extensions.Options;

namespace Afas.Bvr.Core.Tests;

[TestFixture()]
[Property("Dependency", "MSSql")] // Let op, dit helpt om snel tests te filteren, omdat deze een sql dependency hebben
public class MSSqlRepositoryTests : RepositoryTests
{
  private readonly string _connectionString = "Server=.\\profitsqldev;Database=codedidemo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

  public override Repository CreateRepository()
  {
    return new MSSqlRepository(_connectionString);
  }
}

[TestFixture()]
[Property("Dependency", "AzureStorageTable")]
public class AzureStorageTableRepositoryTests : RepositoryTests
{
  public override Repository CreateRepository() => new AzureStorageTableRepository(Options.Create(new AzureStorageTableOptions() { Endpoint = @"https://coredidemobvr.table.core.windows.net/", SasSignature = @"sv=2024-11-04&ss=t&srt=sco&sp=rwdlacu&se=2027-02-02T22:47:53Z&st=2026-02-02T14:32:53Z&spr=https&sig=sfGn0eJpMZn%2BjkEC6xc1Rp7sCprMHufsPvvuegPqDVY%3D" }));
}

public abstract class RepositoryTests
{
  public abstract Repository CreateRepository();

  class Demo : RepositoryObjectWithGuidId
  {
    public string? Name { get; set; }
  }

  [Test()]
  public async Task GetOrDefault_WithoutItem_ReturnsNull()
  {
    var repo = CreateRepository();

    var result = await repo.GetOrDefault<Demo>(Guid.NewGuid());
    Assert.That(result, Is.Null);
  }

  [Test()]
  public async Task Add_RunsWithoutFailure_HasItemInDB()
  {
    var repo = CreateRepository();

    var id = Guid.NewGuid();

    var demo = new Demo { Id = id, Name = "Test" };
    await repo.Add<Demo>(demo);

    var result = await repo.GetOrDefault<Demo>(id);
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Id, Is.EqualTo(id));
    Assert.That(result.Name, Is.EqualTo("Test"));
  }
}
