namespace Afas.Bvr.Core.Repository.Tests;

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
    await repo.Add(demo);

    var result = await repo.GetOrDefault<Demo>(id);
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Id, Is.EqualTo(id));
    Assert.That(result.Name, Is.EqualTo("Test"));
  }
}
