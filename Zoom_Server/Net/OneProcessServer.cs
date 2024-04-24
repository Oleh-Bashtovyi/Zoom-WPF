using Zoom_Server.Logging;

namespace Zoom_Server.Net;

public abstract class OneProcessServer
{
    protected int _port;
    protected string _host;
    protected ILogger log;
    protected Task? _runningProcess;

    //Process
    protected CancellationTokenSource _cancellationTokenSource { get; set; } = new();
    public bool IsRunning => _runningProcess != null && !_runningProcess.IsCompleted;



    public OneProcessServer(string host, int port, ILogger logger)
    {
        _host = host;
        _port = port;
        log = logger;
    }


    #region Run\Stop
    public void Run()
    {
        if (IsRunning)
        {
            throw new Exception("Server is already running!");
        }

        _cancellationTokenSource = new();
        _runningProcess = Task.Run(() => Process(_cancellationTokenSource.Token));
    }
    public void Stop()
    {
        if (!IsRunning)
        {
            throw new Exception("Server is not running!");
        }

        _cancellationTokenSource?.Cancel();
        _runningProcess = null;
    }
    #endregion


    protected abstract Task Process(CancellationToken token);
}
