using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace ReverseProxy_NET6.Proxy;

public class UdpProxy
{
  public UdpProxy(string name,
                  ProxyConfig config) {
    Name = name;
    Config = config;
  }

  /// <summary>
  ///   Milliseconds
  /// </summary>
  public int ConnectionTimeout { get; set; } = 4 * 60 * 1000;

  public ProxyConfig Config { get; }
  public string Name { get; private set; }

  public async Task Start() //string forwardIp, ushort forwardPort, ushort localPort, string? localIp = null,
  {
    var connections = new ConcurrentDictionary<IPEndPoint, UdpConnection>();

    // TCP will lookup every time while this is only once.
    var ips = await Dns.GetHostAddressesAsync(Config.ForwardIp).ConfigureAwait(false);
    var remoteServerEndPoint = new IPEndPoint(ips[0], Config.ForwardPort);
    var result = IPAddress.TryParse(Config.LocalIp, out var localIpAddress);
    if (!result || localIpAddress == null) localIpAddress = IPAddress.IPv6Any;
    var localServer = new UdpClient(AddressFamily.InterNetworkV6);
    if (localIpAddress.AddressFamily == AddressFamily.InterNetwork)
      localServer.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IPOptions, false);
    else if (localIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
      localServer.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
    localServer.Client.Bind(new IPEndPoint(localIpAddress, Config.LocalPort));

    Log.Information("[UDP] [{Name}] Proxy started [{LocalIpAddress}]:{ConfigLocalPort} -> [{ConfigForwardIp}]:{ConfigForwardPort}",
                    Name,
                    localIpAddress,
                    Config.LocalPort,
                    Config.ForwardIp,
                    Config.ForwardPort);
    var _ = Task.Run(async () => {
      while (true) {
        await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        foreach (var connection in connections.ToArray())
          if (connection.Value.LastActivity + ConnectionTimeout < Environment.TickCount64) {
            connections.TryRemove(connection.Key, out var c);
            connection.Value.Stop();
          }
      }
    });

    while (true)
      try {
        var message = await localServer.ReceiveAsync().ConfigureAwait(false);
        var sourceEndPoint = message.RemoteEndPoint;
        var client = connections.GetOrAdd(sourceEndPoint,
                                          ep => {
                                            var udpConnection = new UdpConnection(localServer, sourceEndPoint, remoteServerEndPoint);
                                            udpConnection.Run();
                                            return udpConnection;
                                          });
        await client.SendToServerAsync(message.Buffer).ConfigureAwait(false);
      }
      catch (Exception ex) {
        Log.Error(ex, "[UDP] [{Name}] An exception occurred on receiving a client datagram", Name);
      }
  }
}