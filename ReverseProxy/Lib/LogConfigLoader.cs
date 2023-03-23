namespace ReverseProxy.Lib;

public static class LogConfigLoader
{
    public static void Default()
    {
        EasLogFactory.Configure(x =>
        {
            x.LogFileName = "Proxy_";
            x.ConsoleAppender = true;
            x.ExceptionHideSensitiveInfo = false;
            x.TraceLogging = false;
            x.WebInfoLogging = false;
            x.MinimumLogLevel = EasLogLevel.Information;
            x.LogFolderPath = "Logs";
            x.SeparateLogLevelToFolder = false;
        });
    }

    public static void Debug()
    {
        EasLogFactory.Configure(x =>
        {
            x.LogFileName = "Proxy_";
            x.ConsoleAppender = true;
            x.ExceptionHideSensitiveInfo = false;
            x.TraceLogging = false;
            x.WebInfoLogging = false;
            x.MinimumLogLevel = EasLogLevel.Debug;
            x.LogFolderPath = "Logs";
            x.SeparateLogLevelToFolder = false;
        });
    }

    public static void Release()
    {
        EasLogFactory.Configure(x =>
        {
            x.LogFileName = "Proxy_";
            x.ConsoleAppender = true;
            x.ExceptionHideSensitiveInfo = false;
            x.TraceLogging = false;
            x.WebInfoLogging = false;
            x.MinimumLogLevel = EasLogLevel.Information;
            x.LogFolderPath = "Logs";
            x.SeparateLogLevelToFolder = false;
        });
    }

    public static void ReleaseEfficient()
    {
        EasLogFactory.Configure(x =>
        {
            x.LogFileName = "Proxy_";
            x.ConsoleAppender = true;
            x.ExceptionHideSensitiveInfo = false;
            x.TraceLogging = false;
            x.WebInfoLogging = false;
            x.MinimumLogLevel = EasLogLevel.Warning;
            x.LogFolderPath = "Logs";
            x.SeparateLogLevelToFolder = false;
        });
    }
}