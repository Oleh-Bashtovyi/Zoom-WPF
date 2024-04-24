/*using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public void Log(string message) => _collection.Add(message);
    public void LogError(string message) => _collection.Add(message);
    public void LogSuccess(string message) => _collection.Add(message);
    public void LogUsedCommand(string message) => _collection.Add(message);
    public void LogWarning(string message) => _collection.Add(message);
}
*/