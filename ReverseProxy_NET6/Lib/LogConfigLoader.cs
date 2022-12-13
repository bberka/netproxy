namespace ReverseProxy_NET6.Lib
{
    public static class LogConfigLoader
    {
        public static void Default()
        {
			EasLogFactory.LoadConfig(new EasLogConfiguration
            {
                LogFileName = "Proxy_",
                AddRequestUrlToStart = false,
                ConsoleAppender = true,
                ExceptionHideSensitiveInfo = false,
                TraceLogging = false,
                WebInfoLogging = false,
                LogFileExtension = ".txt",
                IsLogJson = false,
                MinimumLogLevel = Severity.INFO,
                DontLog = false,
				LogFolderPath = "Logs",
                StackLogCount = 0,
                SeperateLogLevelToFolder = false,
			});
        }
        public static void Debug()
        {
			EasLogFactory.LoadConfig(new EasLogConfiguration
            {
                LogFileName = "Proxy_Debug_",
                AddRequestUrlToStart = false,
                ConsoleAppender = true,
                ExceptionHideSensitiveInfo = false,
                TraceLogging = true,
                WebInfoLogging = false,
                LogFileExtension = ".txt",
                IsLogJson = false,
                MinimumLogLevel = Severity.DEBUG
            });
        }
        public static void Release()
        {
			EasLogFactory.LoadConfig(new EasLogConfiguration
            {
                LogFileName = "Proxy_",
                AddRequestUrlToStart = false,
                ConsoleAppender = true,
                ExceptionHideSensitiveInfo = false,
                TraceLogging = true,
                WebInfoLogging = false,
                LogFileExtension = ".txt",
                IsLogJson = false,
                MinimumLogLevel = Severity.WARN
            });
        }
        public static void ReleaseEfficient()
        {
            EasLogFactory.LoadConfig(new EasLogConfiguration
            {
                LogFileName = "Proxy_",
                AddRequestUrlToStart = false,
                ConsoleAppender = true,
                ExceptionHideSensitiveInfo = true,
                TraceLogging = false,
                WebInfoLogging = false,
                LogFileExtension = ".txt",
                IsLogJson = false,
                MinimumLogLevel = Severity.ERROR
            });
        }
    }
}
