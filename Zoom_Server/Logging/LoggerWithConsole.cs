namespace Zoom_Server.Logging;
public class LoggerWithConsole : ILogger
{
    public LoggerWithConsole() { }
    public void Log(string message) => LogMessage(message, Console.ForegroundColor);
    public void LogError(string message) => LogMessage(message, ConsoleColor.Red);
    public void LogSuccess(string message) => LogMessage(message, ConsoleColor.Green);
    public void LogWarning(string message) => LogMessage(message, ConsoleColor.Yellow);
    public void LogUsedCommand(string message) => Log($">>{message}");
    public void ClearOutput() => Console.Clear();
    public virtual void LogMessage(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = ConsoleColor.White;
        Console.ResetColor();
    }
}
public class LoggerWithConsoleAndTime : LoggerWithConsole
{ 
    public override void LogMessage(string message, ConsoleColor color)
    {
        base.LogMessage($"[{DateTime.Now}] - " + message, color);
    }
}
