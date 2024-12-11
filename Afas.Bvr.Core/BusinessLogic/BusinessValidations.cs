using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Afas.Bvr.Core.BusinessLogic;
public static class BusinessValidations
{
  public static void ValidateNotNullOrEmpty([NotNull]string? argument, [CallerArgumentExpression("argument")] string? paramName = null)
  {
    if(string.IsNullOrEmpty(argument))
    {
      throw new Exception($"{paramName} cannot be null or empty");
    }
  }
}
