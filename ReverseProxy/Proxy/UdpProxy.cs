#nullable enable

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ReverseProxy.Connection;

namespace ReverseProxy.Proxy;

public class UdpProxy
{
    private static readonly IEasLog logger = EasLogFactory.CreateLogger();

    public UdpProxy(string name, ProxyConfig config)
    {
        Name = name;
        Config = config;
    }

    /// <summary>
    ///     Milliseconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 4 * 60 * 1000;

    public ProxyConfig Config { get; }
    public string Name { get; }

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

        logger.Info("UDP",
            $"Proxy started [{localIpAddress}]:{Config.LocalPort} -> [{Config.ForwardIp}]:{Config.ForwardPort}");
        var _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                foreach (var connection in connections.ToArray())
                {
                    if (connection.Value.LastActivity + ConnectionTimeout >= Environment.TickCount64) continue;
                    connections.TryRemove(connection.Key, out var c);
                    connection.Value.Stop();
                    AppTitleManager.This.PopKilled();
                }
            }
        });

        while (true)
            try
            {
                var message = await localServer.ReceiveAsync().ConfigureAwait(false);
                var sourceEndPoint = message.RemoteEndPoint;
                var client = connections.GetOrAdd(sourceEndPoint,
                    ep =>
                    {
                        var udpConnection = new UdpConnection(localServer, sourceEndPoint, remoteServerEndPoint);
                        udpConnection.Run();
                        AppTitleManager.This.PopLive();
                        return udpConnection;
                    });
                await client.SendToServerAsync(message.Buffer).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AppTitleManager.This.PopError();
                logger.Exception(ex, "UDP", "An exception occurred on receiving a client datagram.");
            }
    }
}