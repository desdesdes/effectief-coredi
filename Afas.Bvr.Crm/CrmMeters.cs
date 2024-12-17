using System.Diagnostics.Metrics;

namespace Afas.Bvr.Crm;

public class CrmMeters
{
  private readonly Counter<int> _personsAdded;

  public CrmMeters(IMeterFactory meterFactory)
  {
    var meter = meterFactory.Create("Afas.Bvr.Crm");
    _personsAdded = meter.CreateCounter<int>("afas.bvr.crm.persons_added");
  }

  public void PersonsAdded(int quantity)
  {
    _personsAdded.Add(quantity);
  }
}
