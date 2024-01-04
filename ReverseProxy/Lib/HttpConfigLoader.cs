using System.Text.Json;

namespace ReverseProxy.Lib;

public sealed class HttpConfigLoader
{
  
  public static void Load() {
    throw new NotImplementedException();
    try {
      #if RELEASE
      var configJson = File.ReadAllText("config_proxy.json");
      #else
      var configJson = File.ReadAllText("config_proxy.dev.json");
      #endif
      var configs = JsonSerializer.Deserialize<Dictionary<string, HttpConfig>>(configJson);
      ValidateConfig(configs);

    }
    catch (Exception ex) {
      Log.Fatal(ex, "An exception occurred");
      AppTitleManager.This.PopError();
      throw;
    }
  }

  private static void ValidateConfig(Dictionary<string, HttpConfig> configs) {
    throw new NotImplementedException();
  }
}