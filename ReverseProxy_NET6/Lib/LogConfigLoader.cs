namespace ReverseProxy_NET6.Lib
{
    public static class LogConfigLoader
    {
        public static void Default()
        {
            IEasLog.LoadConfig(new EasLogConfiguration
            {
                LogFileName = "Proxy",
                AddRequestUrlToStart = false,
                ConsoleAppender = true,
                ExceptionHideSensitiveInfo = false,
                IsDebug = true,
                TraceLogging = true,
                WebInfoLogging = false,
            });
        }
        public static void Debug()
        {
            IEasLog.LoadConfig(new EasLogConfiguration
            {
                LogFileName = "Proxy_Debug_",
                AddRequestUrlToStart = false,
                ConsoleAppender = true,
                ExceptionHideSensitiveInfo = false,
                IsDebug = true,
                TraceLogging = true,
                WebInfoLogging = false,
            });
        }
        public static void Release()
        {
            IEasLog.LoadConfig(new EasLogConfiguration
            {
                LogFileName = "Proxy_",
                AddRequestUrlToStart = false,
                ConsoleAppender = true,
                ExceptionHideSensitiveInfo = false,
                IsDebug = false,
                TraceLogging = true,
                WebInfoLogging = false,
            });
        }
        public static void ReleaseEfficient()
        {
            IEasLog.LoadConfig(new EasLogConfiguration
            {
                LogFileName = "Proxy_",
                AddRequestUrlToStart = false,
                ConsoleAppender = true,
                ExceptionHideSensitiveInfo = true,
                IsDebug = false,
                TraceLogging = false,
                WebInfoLogging = false,
            });
        }
    }
}
