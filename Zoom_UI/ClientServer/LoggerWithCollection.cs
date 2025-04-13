using System.Collections.ObjectModel;
using System.Windows;
using Zoom_UI.MVVM.Models;
namespace Zoom_Server.Logging;

public class LoggerWithCollection : ILogger
{
    private ObservableCollection<DebugMessage> _collection;

    public LoggerWithCollection(ObservableCollection<DebugMessage> collection)
    {
        _collection = collection;
    }

    public void ClearOutput()
    {
        _collection.Clear();
    }

    public ObservableCollection<DebugMessage> GetBuffer() => _collection;

    public void Log(string message)
    {
        if(Application.Current?.Dispatcher != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _collection.Add(new(message));


                if(_collection.Count > 50)
                {
                    while (_collection.Count > 20)
                    {
                        _collection.RemoveAt(_collection.Count - 1);
                    }
                }
            });
        }
    }
    public void LogError(string message) =>Log("(ERR): " + message);
    public void LogSuccess(string message) => Log("(INF): " + message);
    public void LogUsedCommand(string message) => Log("(>>>): " + message);
    public void LogWarning(string message) => Log( "(WRN): " + message);
}
