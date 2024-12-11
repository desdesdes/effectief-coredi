using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    await _repository.Add<Guid, Person>(person);
  }

  public async Task DeletePerson(Guid id)
  {
    await _repository.Delete<Guid, Person>(id);
  }
}
