using MoonReverseProxy;
using System.Text.Json;

namespace ReverseProxy_NET6.Lib
{
    public static class ProxyConfigLoader
    {
        public static void Load()
        {
            try
            {
#if RELEASE
                var configJson = File.ReadAllText("config.json");
#else
                var configJson = File.ReadAllText("config_dev.json");
#endif
                Dictionary<string, ProxyConfig>? configs = JsonSerializer.Deserialize<Dictionary<string, ProxyConfig>>(configJson);
                ValidateConfig(configs);
                var tasks = configs.SelectMany(c => ProxyFromConfig(c.Key, c.Value));
                Task.WhenAll(tasks).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred : {ex}");
                throw;
            }
        }
        static void ValidateConfig(Dictionary<string, ProxyConfig>? configs)
        {
            if (configs == null) throw new NullReferenceException("Config is NULL");
            
        }
        static IEnumerable<Task> ProxyFromConfig(string proxyName, ProxyConfig proxyConfig)
        {
           
            bool protocolHandled = false;
            if (proxyConfig.Protocol == "udp")
            {
                protocolHandled = true;
                Task task;
                try
                {
                    var proxy = new UdpProxy(proxyName, proxyConfig);
                    task = proxy.Start();
                    Statics.UdpProxies.Add(proxy);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to start {proxyName} : {ex.Message}");
                    throw;
                }
                yield return task;
            }
            if (proxyConfig.Protocol == "tcp" )
            {
                protocolHandled = true;
                Task task;
                try
                {
                    var proxy = new TcpProxy(proxyName, proxyConfig);
                    task = proxy.Start();
                    Statics.TcpProxies.Add(proxy);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to start {proxyName} : {ex.Message}");
                    throw;
                }
                yield return task;
            }

            if (!protocolHandled)
            {
                throw new InvalidOperationException($"protocol not supported {proxyConfig.Protocol}");
            }
        }
    }
}
