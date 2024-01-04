using Serilog;
using Serilog.ConfigHelper;
using Serilog.ConfigHelper.Enricher;
using Serilog.Formatting.Compact;

namespace ReverseProxy.Lib;

public static class LogConfigLoader
{
  private static readonly string LogPath = Path.Combine("Log", "Log.txt");

  public static void Configure() {
    Log.Logger = GetDefaultConfiguration()
      .CreateLogger();
  }

  private static LoggerConfiguration GetDefaultConfiguration() {
    var template = new SerilogTemplateBuilder()
                   .AddTimeStamp()
                   .AddLevel()
                   .AddMessage()
                   .AddException()
                   .Build();
    var config = new LoggerConfiguration()
                 .MinimumLevel.Information()
                 .WriteTo.Console(outputTemplate: template)
                 .WriteTo.File(LogPath,
                               rollingInterval: RollingInterval.Day,
                               retainedFileCountLimit: 14,
                               outputTemplate: template);
    return config;
  }
}