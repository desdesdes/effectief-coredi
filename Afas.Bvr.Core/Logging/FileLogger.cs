namespace Afas.Bvr.Core.Logging;

public class FileLogger : ILogger
{
  private readonly string _filePath;

  public FileLogger(string filePath)
  {
    _filePath = filePath;
  }
  public void Write(string message)
  {
    File.AppendAllText(_filePath, message + Environment.NewLine);
  }
}
