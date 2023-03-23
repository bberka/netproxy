using System.Reflection;

namespace ReverseProxy.Lib;

public class AppTitleManager
{
    private static Thread _thread;
    private static AppTitleManager? Instance;
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private static readonly string _version = _assembly.GetName().Version?.ToString() ?? "...";

    private static readonly string _title = "Reverse_Proxy_NET6 v" + _version +
                                            " - Listener({0}) Total({1}) Live({2}) Blocked({3}) Load({4}) Error({5}) Killed({6}) Forwarded({7}) Responded({8})";

    private static AppTitleData _data = new();

    private AppTitleManager()
    {
        _thread = new Thread(UpdateLoopProcess)
        {
            IsBackground = true
        };
        _thread.Start();
    }

    public static AppTitleManager This
    {
        get
        {
            Instance ??= new AppTitleManager();
            return Instance;
        }
    }

    private static void UpdateConsoleTitle(string str)
    {
        Console.Title = str;
    }

    private static void Update(AppTitleData data)
    {
        _data = data;
        var format = string.Format(_title, data.Listener, data.Total, data.Live, data.Blocked, data.Load, data.Error,data.Killed, data.BytesForwarded,data.BytesResponded);
        UpdateConsoleTitle(format);
    }

    private void UpdateLoopProcess()
    {
        while (true)
        {
            Update(_data);
            Thread.Sleep(500);
        }
    }

    public void SetLive(int live)
    {
        _data.Live = live;
    }


    public void SetLoad(int load)
    {
        _data.Load = load;
    }



    public void PopError()
    {
        _data.Error++;
    }


    public void PopBlocked()
    {
        _data.Blocked++;
    }

    public void PopLive()
    {
        _data.Live++;
        _data.Total++;

    }

    public void PopListener()
    {
        _data.Listener++;
    }

    public void PopKilled()
    {
        _data.Killed++;
    }

    public void PopForwarded(long forwarded)
    {
        _data.BytesForwarded += Convert.ToUInt64(forwarded);
    }

    public void PopResponded(long responded)
    {
        _data.BytesResponded += Convert.ToUInt64(responded);
    }


    public void DecreaseLive()
    {
        _data.Live--;
    }


    public AppTitleData Get()
    {
        return _data;
    }
}