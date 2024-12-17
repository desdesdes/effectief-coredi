using System.Diagnostics.Metrics;
using Afas.Bvr.Core.BusinessLogic;
using Afas.Bvr.Core.Repository;
using Microsoft.Extensions.Logging;

namespace Afas.Bvr.Crm;

public class PersonBC
{
  private readonly Repository _repository;
  private readonly ILogger _logger;
  private readonly TimeProvider _timeProvider;
  private readonly PhonenumberChecker _phonenumberChecker;
  private readonly CrmMeters _meters;

  public PersonBC(Repository repository, ILogger<PersonBC> logger, TimeProvider timeProvider, PhonenumberChecker phonenumberChecker, CrmMeters meters)
  {
    _repository = repository;
    _logger = logger;
    _timeProvider = timeProvider;
    _phonenumberChecker = phonenumberChecker;
    _meters = meters;
  }

  public async Task AddPerson(Person person)
  {
    _logger.LogAddPerson(person.Id);

    BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrSpaces(person.FirstName);
    BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrSpaces(person.LastName);

    CrmValidations.ValidatePhoneNumber(person.PhoneNumber);

    if(await _phonenumberChecker.CheckPhoneNumber(person.PhoneNumber))
    {
      throw new Exception("PhoneNumber cannot be in the future.");
    }

    if(person.BirthDate.HasValue && person.BirthDate.Value.ToDateTime(TimeOnly.MinValue) > _timeProvider.GetLocalNow())
    {
      throw new Exception("BirthDate cannot be in the future.");
    }

    await _repository.Add(person);
    _meters.PersonsAdded(1);
  }

  public async Task DeletePerson(Guid id)
  {
    _logger.LogInformation($"PersonBC: DeletePerson '{id}'");

    await _repository.Delete<Person>(id);
  }

  public async Task<Person?> GetPersonOrDefault(Guid id)
  {
    _logger.LogInformation($"PersonBC: GetPersonOrDefault '{id}'");

    return await _repository.GetOrDefault<Person>(id);
  }
}

internal static partial class ILoggerExtensions
{
  [LoggerMessage(Message = "Person added {id}", Level = LogLevel.Critical)]
  public static partial void LogAddPerson(this ILogger logger, Guid id);
}
