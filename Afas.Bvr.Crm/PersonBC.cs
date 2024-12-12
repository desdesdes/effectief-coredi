using Afas.Bvr.Core.BusinessLogic;
using Afas.Bvr.Core.Repository;

namespace Afas.Bvr.Crm;

public class PersonBC
{
  private readonly Repository _repository;

  public PersonBC(Repository repository)
  {
    _repository = repository;
  }

  public async Task AddPerson(Person person)
  {
    BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrSpaces(person.FirstName);
    BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrSpaces(person.LastName);

    await _repository.Add<Guid, Person>(person);
  }

  public async Task DeletePerson(Guid id)
  {
    await _repository.Delete<Guid, Person>(id);
  }
}
