using FakeItEasy;
using Afas.Bvr.Core.Repository;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Afas.Bvr.Crm.Tests;

[TestFixture()]
public class PersonBCTests
{
  private FakeTimeProvider CreateTimeProvider()
  {
    return new FakeTimeProvider(new DateTimeOffset(2024, 12, 19, 10, 15, 32, new TimeSpan(1, 0, 0)));
  }

  private Person CreatePerson(Action<Person>? changes = null)
  {
    var p = new Person() { Id = Guid.NewGuid(), FirstName = "Bart", LastName = "Vries" };
    changes?.Invoke(p);
    return p;
  }

  [Test()]
  public void AddPerson_WithBirthDateInFuture_ThrowExceptions()
  {
    var bc = new PersonBC(A.Fake<Repository>(), NullLogger<PersonBC>.Instance, CreateTimeProvider(), A.Fake<PhonenumberChecker>(), A.Fake<CrmMeters>());

    Assert.ThrowsAsync<Exception>(() => bc.AddPerson(CreatePerson(p => p.BirthDate = new DateOnly(2025, 12, 19))));
  }

  [Test()]
  public void AddPerson_WithBirthDateInPast_Succeeds()
  {
    var testRepository = A.Fake<Repository>();
    var bc = new PersonBC(testRepository, NullLogger<PersonBC>.Instance, CreateTimeProvider(), A.Fake<PhonenumberChecker>(), A.Fake<CrmMeters>());

    Assert.DoesNotThrowAsync(() => bc.AddPerson(CreatePerson(p => p.BirthDate =new DateOnly(2023,12,19))));
    A.CallTo(() => testRepository.Add(A<Person>._)).MustHaveHappenedOnceExactly();
  }

  [Test()]
  public void AddPerson_WithFilledValidPhoneNumber_Succeeds()
  {
    var testRepository = A.Fake<Repository>();
    var bc = new PersonBC(testRepository, NullLogger<PersonBC>.Instance, CreateTimeProvider(), A.Fake<PhonenumberChecker>(), A.Fake<CrmMeters>());

    Assert.DoesNotThrowAsync(() => bc.AddPerson(CreatePerson(p => p.PhoneNumber = "(06) 11")));
    A.CallTo(() => testRepository.Add(A<Person>._)).MustHaveHappenedOnceExactly();
  }

  [Test()]
  public void AddPerson_FirstNameStartWithSpace_ThrowExceptions()
  {
    var bc = new PersonBC(A.Fake<Repository>(), NullLogger<PersonBC>.Instance, CreateTimeProvider(), A.Fake<PhonenumberChecker>(), A.Fake<CrmMeters>());
    
    Assert.ThrowsAsync<Exception>(() => bc.AddPerson(CreatePerson(p => p.FirstName = " Bart")));
  }

  [Test()]
  public void AddPerson_WithProperData_Succeeds()
  {
    var testRepository = A.Fake<Repository>();
    var bc = new PersonBC(testRepository, NullLogger<PersonBC>.Instance, CreateTimeProvider(), A.Fake<PhonenumberChecker>(), A.Fake<CrmMeters>());

    Assert.DoesNotThrowAsync(() => bc.AddPerson(CreatePerson()));
    A.CallTo(() => testRepository.Add(A<Person>._)).MustHaveHappenedOnceExactly();
  }
}
