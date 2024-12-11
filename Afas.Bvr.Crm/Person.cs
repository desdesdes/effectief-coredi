using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Afas.Bvr.Core.Repository;

namespace Afas.Bvr.Crm;

public class Person : RepositoryObject<Guid>
{
  public string? FirstName { get; set; }
  public string? LastName { get; set; }
  public string? Email { get; set; }
  public string? PhoneNumber { get; set; }
}
