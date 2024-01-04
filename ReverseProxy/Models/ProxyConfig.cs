namespace ReverseProxy.Models;

public class ProxyConfig
{
  public string Protocol { get; set; } = null!;
  public ushort LocalPort { get; set; }
  public string LocalIp { get; set; } = null!;
  public string ForwardIp { get; set; } = null!;
  public ushort ForwardPort { get; set; }
  public long MaxConnectionLimit { get; set; } = 0;
  public long ConnectionLimitPerIp { get; set; } = 0;
  public List<string> FilterConnection { get; set; } = new();
  public ushort RequireConnectionToPort { get; set; } = 0;
}