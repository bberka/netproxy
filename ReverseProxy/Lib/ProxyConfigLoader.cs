using System.Text.Json;

namespace ReverseProxy.Lib;

public static class ProxyConfigLoader
{

  public static void Load() {
    try {
      #if RELEASE
      var configJson = File.ReadAllText("config_proxy.json");
      #else
      var configJson = File.ReadAllText("config_proxy.dev.json");
      #endif
      var configs = JsonSerializer.Deserialize<Dictionary<string, ProxyConfig>>(configJson);
      ValidateConfig(configs);
      var tasks = configs.SelectMany(c => ProxyFromConfig(c.Key, c.Value));

      Task.WhenAll(tasks).Wait();
    }
    catch (Exception ex) {
      Log.Fatal(ex, "An exception occurred");
      AppTitleManager.This.PopError();
      throw;
    }
  }

  private static void ValidateConfig(Dictionary<string, ProxyConfig>? configs) {
    if (configs == null) throw new NullReferenceException("Config is NULL");
  }

  private static IEnumerable<Task> ProxyFromConfig(string proxyName, ProxyConfig proxyConfig) {
    var protocolHandled = false;
    if (proxyConfig.Protocol == "udp") {
      protocolHandled = true;
      Task task;
      try {
        var proxy = new UdpProxy(proxyName, proxyConfig);
        lock (Statics.UdpProxies) {
          Statics.UdpProxies.Add(proxy);
        }

        task = proxy.Start();
        AppTitleManager.This.PopListener();
      }
      catch (Exception ex) {
        Log.Fatal(ex, "Failed to start UDP Proxy {ProxyName}", proxyName);
        AppTitleManager.This.PopError();
        throw;
      }

      yield return task;
    }

    if (proxyConfig.Protocol == "tcp") {
      protocolHandled = true;
      Task task;
      try {
        var proxy = new TcpProxy(proxyName, proxyConfig);
        lock (Statics.TcpProxies) {
          Statics.TcpProxies.Add(proxy);
        }

        task = proxy.Start();
        AppTitleManager.This.PopListener();
      }
      catch (Exception ex) {
        Log.Fatal(ex, "Failed to start TCP Proxy {ProxyName}", proxyName);
        AppTitleManager.This.PopError();
        throw;
      }

      yield return task;
    }

    if (!protocolHandled) throw new InvalidOperationException($"protocol not supported {proxyConfig.Protocol}");
  }
}