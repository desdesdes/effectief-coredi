using Afas.Bvr.Core.Storage;
using Afas.Bvr.Crm;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Afas.Bvr.Core.Tests;

[TestFixture()]
public class PersonBCTests
{
  [Test()]
  public void AddPerson_FirstNameStartWithSpace_ThrowsException()
  {
    var bc = new PersonBC(A.Fake<Repository>(), NullLogger<PersonBC>.Instance, CreateTimeProvider(), A.Fake<CrmDependencies>(), A.Fake<CrmMeters>());

    Assert.ThrowsAsync<Exception>(() => bc.AddPerson(new Person() { Id = Guid.NewGuid(), FirstName = " Bart", LastName = "Vries" }));
  }

  [Test()]
  public void AddPerson_WithProperData_Succeeds()
  {
    var testRepository = A.Fake<Repository>();

    var bc = new PersonBC(testRepository, NullLogger<PersonBC>.Instance, CreateTimeProvider(), A.Fake<CrmDependencies>(), A.Fake<CrmMeters>());

    Assert.DoesNotThrowAsync(() => bc.AddPerson(new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries" }));
    A.CallTo(() => testRepository.Add(A<Person>._)).MustHaveHappenedOnceExactly();
  }

  private FakeTimeProvider CreateTimeProvider()
  {
    return new FakeTimeProvider(new DateTimeOffset(2024, 12, 19, 10, 15, 32, new TimeSpan(1, 0, 0)));
  }

  [Test()]
  public void AddPerson_WithBirthDateInFuture_ThrowExceptions()
  {
    var bc = new PersonBC(A.Fake<Repository>(), NullLogger<PersonBC>.Instance, CreateTimeProvider(), A.Fake<CrmDependencies>(), A.Fake<CrmMeters>());

    Assert.ThrowsAsync<Exception>(() => bc.AddPerson(new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries", BirthDate = new DateOnly(2025, 12, 19) }));
  }

  [Test()]
  public void AddPerson_WithBirthDateInPast_Succeeds()
  {
    var testRepository = A.Fake<Repository>();
    var bc = new PersonBC(testRepository, NullLogger<PersonBC>.Instance, CreateTimeProvider(), A.Fake<CrmDependencies>(), A.Fake<CrmMeters>());

    Assert.DoesNotThrowAsync(() => bc.AddPerson(new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries", BirthDate = new DateOnly(2023, 12, 19) }));
    A.CallTo(() => testRepository.Add(A<Person>._)).MustHaveHappenedOnceExactly();
  }
}
