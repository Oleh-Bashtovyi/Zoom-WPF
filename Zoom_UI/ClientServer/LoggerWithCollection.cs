using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Zoom_Server.Logging;

public class LoggerWithCollection : ILogger
{
    private Collection<string> _collection;

    public LoggerWithCollection(Collection<string> collection)
    {
        _collection = collection;
    }

    public void ClearOutput()
    {
        _collection.Clear();
    }

    public void Log(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _collection.Add(message);
        });
/*        Dispatcher.CurrentDispatcher.Invoke(() =>
        {
            _collection.Add(message);
        });*/
    }
    public void LogError(string message) =>Log(message);
    public void LogSuccess(string message) => Log(message);
    public void LogUsedCommand(string message) => Log(message);
    public void LogWarning(string message) => Log(message);
}
