
using MoonReverseProxy;

namespace ReverseProxy_NET6.Lib
{
    public static class ProxyValidator
    {
        public static bool ValidateConnection(TcpProxy proxy, TcpConnection tcp, out string reason)
        {
            reason = "NaN";
            if (proxy.Config.FilterConnection.Count != 0)
            {
                foreach (var ip in proxy.Config.FilterConnection)
                {
                    if (ip != tcp.ClientEndPoint.GetIpAddress())
                    {
                        reason = "IP is not allowed";
                        return false;
                    }
                }
            }
            if (proxy.Config.MaxConnectionLimit != 0)
            {
                if (proxy.Connections.Count >= proxy.Config.MaxConnectionLimit)
                {
                    reason = "max connection limit reached";
                    return false;
                }
            }
            if (proxy.Config.ConnectionLimitPerIp != 0)
            {
                var count = proxy.Connections.Where(x => x.ClientEndPoint.GetIpAddress() == tcp.ClientEndPoint.GetIpAddress()).Count();
                if (count >= proxy.Config.ConnectionLimitPerIp)
                {
                    reason = "max connection limit per IP reached";
                    return false;
                }
            }
            if (proxy.Config.RequireConnectionToPort != 0)
            {
                var proxies = Statics.TcpProxies.FirstOrDefault(x => x.Config.LocalPort == proxy.Config.RequireConnectionToPort);
                if (proxies is null)
                {
                    reason = "proxy is NULL";
                    return false;
                }
                if (!proxies.Connections.Any(x => x.ClientEndPoint.GetIpAddress() == tcp.ClientEndPoint.GetIpAddress()))
                {
                    reason = "must connect to " + proxy.Config.RequireConnectionToPort + " port first";
                    return false;
                }
            }

            return true;

        }
        public static bool ValidateConnection(ProxyConfig config, UdpConnection udp)
        {
            return true;
        }
    }
}
