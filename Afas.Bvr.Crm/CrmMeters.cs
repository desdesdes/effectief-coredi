using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace Afas.Bvr.Crm;

public class CrmMeters
{
  private readonly Counter<int> _personsAdded;

  public CrmMeters(IMeterFactory meterFactory)
  {
    var meter = meterFactory.Create("Afas.Bvr.Crm");
    _personsAdded = meter.CreateCounter<int>("afas.bvr.crm.persons_added");
  }

  public virtual void PersonsAdded(int quantity)
  {
    _personsAdded.Add(quantity);
  }
}
