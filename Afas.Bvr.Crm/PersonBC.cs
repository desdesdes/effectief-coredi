using Afas.Bvr.Core.BusinessLogic;
using Afas.Bvr.Core.Storage;
using Microsoft.Extensions.Logging;

namespace Afas.Bvr.Crm;

public partial class PersonBC
{
  private readonly Repository _repository;
  private readonly ILogger _logger;
  private readonly TimeProvider _timeProvider;
  private readonly CrmDependencies _depManager;
  private readonly CrmMeters _meters;

  [LoggerMessage(Message = "Person added {id}", Level = LogLevel.Critical)]
  public partial void LogAddPerson(Guid id);

  public PersonBC(Repository repository, ILogger<PersonBC> logger, TimeProvider timeProvider, CrmDependencies depManager, CrmMeters meters)
  {
    _repository = repository;
    _logger = logger;
    _timeProvider = timeProvider;
    _depManager = depManager;
    _meters = meters;
  }

  public async Task AddPerson(Person person)
  {
    _logger?.LogInformation($"PersonBC: AddPerson '{person.Id}'");

    await _depManager.ValidateExternalUserNameAsync(person.FirstName, person.LastName);

    BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrSpaces(person.FirstName);
    BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrSpaces(person.LastName);

    CrmValidations.ValidatePhoneNumber(person.PhoneNumber);

    if(person.BirthDate.HasValue && person.BirthDate.Value.ToDateTime(TimeOnly.MinValue) > _timeProvider.GetLocalNow())
    {
      throw new Exception("BirthDate cannot be in the future.");
    }

    await _repository.Add(person);
    _meters.PersonsAdded(1);
  }

  public async Task DeletePerson(Guid id)
  {
    LogAddPerson(id);

    await _repository.Delete<Person>(id);
  }

  public async Task<Person?> GetPersonOrDefault(Guid id)
  {
    _logger?.LogInformation($"PersonBC: GetPersonOrDefault '{id}'");

    return await _repository.GetOrDefault<Person>(id);
  }
}
