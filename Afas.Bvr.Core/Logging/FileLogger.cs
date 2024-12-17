namespace Afas.Bvr.Core.Logging;

/// <threadsafety static="true" instance="true"/>
public class FileLogger : ILogger
{
  private readonly string _filePath;

  public FileLogger(string filePath)
  {
    _filePath = filePath;
  }
  public void LogInformation(string message)
  {
    File.AppendAllText(_filePath, message + Environment.NewLine);
  }
}
