namespace Afas.Bvr.Core.BusinessLogic.Tests;

[TestFixture()]
public class BusinessValidationsTests
{
  [TestCase(null, false)]
  [TestCase("", false)]
  [TestCase(" ", true)]
  [TestCase("😉", true)]
  [TestCase("\r", true)]
  [TestCase("\n", true)]
  [TestCase("\n\r", true)]
  public void NotNullOrEmptyTests(string? input, bool success)
  {
    if(success)
    {
      Assert.DoesNotThrow(() => BusinessValidations.NotNullOrEmpty(input));

    }
    else
    {
      Assert.Throws<Exception>(() => BusinessValidations.NotNullOrEmpty(input));
    }
  }

  [TestCase(null, false)]
  [TestCase("", false)]
  [TestCase(" ", false)]
  [TestCase("😉", false)]
  [TestCase("\r", true)]
  [TestCase("\n", true)]
  [TestCase("\n\r", true)]
  [TestCase(" Bart", false)]
  [TestCase("Bart ", false)]
  [TestCase("Bart", true)]
  [TestCase("Bart Vries", true)]
  [TestCase("Bart 1", true)]
  public void NoStartOrEndSpacesAndOnlyLettersOrNumbersOrSpacesTests(string? input, bool success)
  {
    if(success)
    {
      Assert.DoesNotThrow(() =>
              BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrNumbersOrSpaces(input));

    }
    else
    {
      Assert.Throws<Exception>(() =>
                    BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrNumbersOrSpaces(input));
    }
  }


  [TestCase(null, false)]
  [TestCase("", false)]
  [TestCase(" ", false)]
  [TestCase("😉", false)]
  [TestCase("\r", true)]
  [TestCase("\n", true)]
  [TestCase("\n\r", true)]
  [TestCase(" Bart", false)]
  [TestCase("Bart ", false)]
  [TestCase("Bart", true)]
  [TestCase("Bart Vries", true)]
  [TestCase("Bart 1", false)]
  public void NoStartOrEndSpacesAndOnlyLettersOrSpacesTests(string? input, bool success)
  {
    if(success)
    {
      Assert.DoesNotThrow(() =>
              BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrSpaces(input));

    }
    else
    {
      Assert.Throws<Exception>(() =>
                    BusinessValidations.NoStartOrEndSpacesAndOnlyLettersOrSpaces(input));
    }
  }
}
