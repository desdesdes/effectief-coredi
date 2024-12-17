using Afas.Bvr.Crm;

namespace Afas.Bvr.Core.Tests;

[TestFixture()]
public class CrmValidationsTests
{
  [TestCase(null, true)]
  [TestCase("", false)]
  [TestCase(" ", false)]
  [TestCase("😉", false)]
  [TestCase("\r", false)]
  [TestCase("\n", false)]
  [TestCase("\n\r", false)]
  [TestCase("(033) 43 41 800", true)]
  [TestCase("(033) 43 41 800 ", false)]
  [TestCase(" (033) 43 41 800", false)]
  public void ValidatePhoneNumberTests(string? input, bool success)
  {
    if(success)
    {
      Assert.DoesNotThrow(() => CrmValidations.ValidatePhoneNumber(input));

    }
    else
    {
      Assert.Throws<Exception>(() => CrmValidations.ValidatePhoneNumber(input));
    }
  }
}
