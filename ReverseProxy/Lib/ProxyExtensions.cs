using System.Net;

namespace ReverseProxy.Lib;

public static class ProxyExtensions
{
  public static string? GetIpAddress(this EndPoint? endPoint) {
    var split = endPoint?.ToString()?.Split(':');
    if (split == null) return null;
    if (split.Length == 0) return null;
    var ip = split[0];
    if (IPAddress.TryParse(ip, out var ipModel)) return ipModel.ToString();
    return null;
  }

  public static int? GetPort(this EndPoint? endPoint) {
    var split = endPoint?.ToString()?.Split(':');
    if (split == null) return null;
    if (split.Length == 0) return null;
    var ip = split[1];
    if (int.TryParse(ip, out var ipModel)) return ipModel;
    return null;
  }
}