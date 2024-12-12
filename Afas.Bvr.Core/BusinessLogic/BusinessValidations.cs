using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Afas.Bvr.Core.BusinessLogic;

public static partial class BusinessValidations
{
  public static void NotNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression("argument")] string? paramName = null)
  {
    if(string.IsNullOrEmpty(argument))
    {
      throw new Exception($"{paramName} cannot be null or empty");
    }
  }

  [GeneratedRegex(@"^(?! )[\w\s]+(?<! )$")]
  private static partial Regex NoStartOrEndSpacesAndOnlyLettersOrNumbersOrSpacesRegEx();

  public static void NoStartOrEndSpacesAndOnlyLettersOrNumbersOrSpaces(string? input)
  {
    if(input == null || !NoStartOrEndSpacesAndOnlyLettersOrNumbersOrSpacesRegEx().IsMatch(input))
    {
      throw new Exception("String must not start or end with a space and must not contain only letters, numbers or spaces.");
    }
  }

  [GeneratedRegex(@"^(?! )[\p{L}\p{M}\s]+(?<! )$")]
  private static partial Regex NoStartOrEndSpacesAndOnlyLettersOrSpacesRegEx();

  public static void NoStartOrEndSpacesAndOnlyLettersOrSpaces(string? input)
  {
    if(input == null || !NoStartOrEndSpacesAndOnlyLettersOrSpacesRegEx().IsMatch(input))
    {
      throw new Exception("String must not start or end with a space and must not contain only letters or spaces.");
    }
  }
}
