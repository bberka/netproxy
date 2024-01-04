namespace MoonReverseProxy;

public static class Statics
{
  public static List<UdpProxy> UdpProxies { get; set; } = new();
  public static List<TcpProxy> TcpProxies { get; set; } = new();
}

public enum Direction
{
  Unknown = 0,
  Forward,
  Responding
}