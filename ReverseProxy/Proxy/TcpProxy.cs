#nullable enable
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ReverseProxy.Connection;

namespace ReverseProxy.Proxy;

public class TcpProxy
{
    private static readonly IEasLog logger = EasLogFactory.CreateLogger();

    public TcpProxy(string name, ProxyConfig config)
    {
        Name = name;
        Config = config;
    }

    /// <summary>
    ///     Milliseconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 4 * 60 * 1000;

    public ProxyConfig Config { get; }
    public ConcurrentBag<TcpConnection> Connections { get; } = new();
    public string Name { get; }

    /// <summary>
    ///     Initiates the TCP proxy and starts listening with the config in constructor.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task Start()
    {
        var result = IPAddress.TryParse(Config.LocalIp, out var localIpAddress);
        if (!result || localIpAddress == null) throw new Exception($"[TCP] [{Name}] Invalid localIp: {Config.LocalIp}");
        var localServer = new TcpListener(new IPEndPoint(localIpAddress, Config.LocalPort));
        //localServer.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

        localServer.Start();
        logger.Info("TCP", Name,
            $"Proxy started [{localIpAddress}]:{Config.LocalPort} -> [{Config.ForwardIp}]:{Config.ForwardPort}");
        var _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                var tempConnections = new List<TcpConnection>(Connections.Count);
                while (Connections.TryTake(out var connection)) tempConnections.Add(connection);

                foreach (var tcpConnection in tempConnections)
                    if (tcpConnection.LastActivity + ConnectionTimeout < Environment.TickCount64)
                    {
                        AppTitleManager.This.PopKilled();
                        tcpConnection.Stop(Name);
                    }
                    else
                    {
                        Connections.Add(tcpConnection);
                        AppTitleManager.This.SetLive(Connections.Count);
                    }

            }
        });

        while (true)
            try
            {
                var ips = await Dns.GetHostAddressesAsync(Config.ForwardIp).ConfigureAwait(false);
                var tcpConnection = await TcpConnection
                    .AcceptTcpClientAsync(localServer, new IPEndPoint(ips[0], Config.ForwardPort))
                    .ConfigureAwait(false);
                if (!ConnValidator.Validate(this, tcpConnection, out var reason))
                {
                    logger.Error("TCP", Name,
                        $"[{Config.LocalIp}:{Config.LocalPort}] [{tcpConnection.ClientEndPoint.GetIpAddress()}] Connection blocked due to {reason}");
                    tcpConnection.Client.Close();
                    AppTitleManager.This.PopBlocked();
                    continue;
                }

                Connections.Add(tcpConnection);
                tcpConnection.Run(Name);
                AppTitleManager.This.PopLive();
            }
            catch (Exception ex)
            {
                AppTitleManager.This.PopError();
                logger.Exception(ex, "TCP", Name, $"{Config.LocalIp}:{Config.LocalPort}");
            }
    }
}