namespace Afas.Bvr.Core.Logging;

/// <threadsafety static="true" instance="true"/>
public class ConsoleLogger : ILogger
{
  public void LogInformation(string message) => Console.WriteLine(message);
}
