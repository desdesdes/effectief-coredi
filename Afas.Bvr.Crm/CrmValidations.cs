using System.Text.RegularExpressions;

namespace Afas.Bvr.Crm;

internal static partial class CrmValidations
{
  [GeneratedRegex(@"^(?! )[0-9(), ]+(?<! )$")]
  private static partial Regex ValidatePhoneNumberRegEx();

  public static void ValidatePhoneNumber(string? input)
  {
    if(input != null && !ValidatePhoneNumberRegEx().IsMatch(input))
    {
      throw new Exception("String must not start or end with a space and can only contain numbers, (, ) or spaces.");
    }
  }
}
