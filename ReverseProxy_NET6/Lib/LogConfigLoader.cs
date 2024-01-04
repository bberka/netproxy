using Serilog.Events;
using Serilog.Formatting.Compact;

namespace ReverseProxy_NET6.Lib;

public static class LogConfigLoader
{
  public static void Configure() {
    const string template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
    const string logPath = "logs/log.txt";
    const LogEventLevel logLevel = LogEventLevel.Information;
    var serilogConfig = new Serilog.LoggerConfiguration()
                        .MinimumLevel.Is(logLevel)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(logLevel, template)
                        .WriteTo.File(new CompactJsonFormatter(), logPath);
    Log.Logger = serilogConfig.CreateLogger();
  }
}