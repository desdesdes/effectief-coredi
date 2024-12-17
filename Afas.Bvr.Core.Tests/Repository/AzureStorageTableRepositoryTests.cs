using Microsoft.Extensions.Options;

namespace Afas.Bvr.Core.Repository.Tests;

[TestFixture()]
[Category("Dep:AzureStorageTable")]
public class AzureStorageTableRepositoryTests : RepositoryTests
{
  public override Repository CreateRepository()
  {
    var set = Options.Create<AzureStorageTableSettings>(new AzureStorageTableSettings() {
      Endpoint = @"https://codedidemo.table.core.windows.net/",
      SasSignature = @"sv=2022-11-02&ss=t&srt=sco&sp=rwdlacu&se=2028-12-11T23:55:39Z&st=2024-12-11T15:55:39Z&spr=https&sig=e684bQmmbwMXysmGBlbIlA4h365DFVDlJa1nVVeINOk%3D"
    });

    return new AzureStorageTableRepository(set);
  }
}
