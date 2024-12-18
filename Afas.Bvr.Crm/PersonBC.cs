using Afas.Bvr.Core.BusinessLogic;
using Afas.Bvr.Core.Logging;
using Afas.Bvr.Core.Repository;

namespace Afas.Bvr.Crm;

public class PersonBC
{
  private readonly Repository _repository;
  private readonly ILogger? _logger;

  public PersonBC(StorageSettings settings, ILogger? logger = null)
  {
    _repository = Repository.CreateRepository(settings);
    _logger = logger;
  }

  public async Task AddPerson(Person person)
  {
    _logger?.LogInformation($"PersonBC: AddPerson '{person.Id}'");

    BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrSpaces(person.FirstName);
    BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrSpaces(person.LastName);

    CrmValidations.ValidatePhoneNumber(person.PhoneNumber);

    await _repository.Add<Guid, Person>(person);
  }

  public async Task DeletePerson(Guid id)
  {
    _logger?.LogInformation($"PersonBC: DeletePerson '{id}'");

    await _repository.Delete<Guid, Person>(id);
  }

  public async Task<Person?> GetPersonOrDefault(Guid id)
  {
    _logger?.LogInformation($"PersonBC: GetPersonOrDefault '{id}'");

    return await _repository.GetOrDefault<Guid, Person>(id);
  }
}
