namespace Zoom_Server.Logging;

public interface ILogger
{
    public void ClearOutput();
    public void Log(string message);
    public void LogError(string message);
    public void LogWarning(string message);
    public void LogSuccess(string message);
    public void LogUsedCommand(string message);
    public void HandleLogWithIdentificators(string message);
}
