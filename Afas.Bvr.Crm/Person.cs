using Afas.Bvr.Core.Repository;

namespace Afas.Bvr.Crm;

public class Person : RepositoryObjectWithGuidId
{
  public string? FirstName { get; set; }
  public string? LastName { get; set; }
  public string? Email { get; set; }
  public string? PhoneNumber { get; set; }
  public DateOnly? BirthDate { get; set; }
}
