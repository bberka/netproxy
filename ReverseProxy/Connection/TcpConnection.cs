using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace ReverseProxy.Connection;

public class TcpConnection
{
    private static readonly IEasLog logger = EasLogFactory.CreateLogger();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IPEndPoint _remoteEndpoint;
    private readonly EndPoint? _serverLocalEndpoint;
    private long _totalBytesForwarded;
    private long _totalBytesResponded;

    private TcpConnection(TcpClient localServerConnection, IPEndPoint remoteEndpoint)
    {
        ConnectedAt = DateTime.Now;
        Client = localServerConnection;
        _remoteEndpoint = remoteEndpoint;

        Destination = new TcpClient { NoDelay = true };

        ClientEndPoint = Client.Client.RemoteEndPoint;
        _serverLocalEndpoint = Client.Client.LocalEndPoint;
    }

    public TcpClient Client { get; }

    public TcpClient Destination { get; }

    public EndPoint? ClientEndPoint { get; }

    public EndPoint? DestinationEndPoint { get; private set; }

    public long TotalBytesForwarded => _totalBytesForwarded;

    public long TotalBytesResponded => _totalBytesResponded;

    public DateTime ConnectedAt { get; }
    public long LastActivity { get; private set; } = Environment.TickCount64;

    public static async Task<TcpConnection> AcceptTcpClientAsync(TcpListener tcpListener, IPEndPoint remoteEndpoint)
    {
        var localServerConnection = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
        localServerConnection.NoDelay = true;
        return new TcpConnection(localServerConnection, remoteEndpoint);
    }

    public void Run(string name)
    {
        RunInternal(_cancellationTokenSource.Token, name);
    }

    public void Stop(string name)
    {
        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (Exception ex)
        {
            AppTitleManager.This.PopError();
            logger.Exception(ex, "TCP", name, "An exception occurred while closing TcpConnection.");
        }
    }

    private void RunInternal(CancellationToken cancellationToken, string name)
    {
        Task.Run(async () =>
        {
            try
            {
                using (Client)
                using (Destination)
                {
                    await Destination.ConnectAsync(_remoteEndpoint.Address, _remoteEndpoint.Port, cancellationToken)
                        .ConfigureAwait(false);
                    DestinationEndPoint = Destination.Client.LocalEndPoint;
                    logger.Info("TCP", name, "ESTABLISHED",
                        $"{ClientEndPoint} => {_serverLocalEndpoint} => {DestinationEndPoint} => {_remoteEndpoint}");

                    using (var serverStream = Destination.GetStream())
                    using (var clientStream = Client.GetStream())
                    using (cancellationToken.Register(() =>
                           {
                               serverStream.Close();
                               clientStream.Close();
                           }, true))
                    {
                        await Task.WhenAny(
                            CopyToAsync(clientStream, serverStream, 81920, Direction.Forward, cancellationToken),
                            CopyToAsync(serverStream, clientStream, 81920, Direction.Responding, cancellationToken)
                        ).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppTitleManager.This.PopError();
                logger.Exception(ex, "TCP", name, "An exception occurred during TCP stream.");
            }
            finally
            {
                AppTitleManager.This.DecreaseLive();
                logger.Warn("TCP", name,
                    $"Connection {ClientEndPoint} => {_serverLocalEndpoint} => {DestinationEndPoint} => {_remoteEndpoint}. Forwarded({_totalBytesForwarded})/Responded({_totalBytesResponded}) bytes.");
            }
        });
    }

    private async Task CopyToAsync(Stream source, Stream destination, int bufferSize = 81920,
        Direction direction = Direction.Unknown, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            while (true)
            {
                var bytesRead = await source.ReadAsync(new Memory<byte>(buffer), cancellationToken)
                    .ConfigureAwait(false);
                if (bytesRead == 0) break;
                LastActivity = Environment.TickCount64;
                await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken)
                    .ConfigureAwait(false);
                switch (direction)
                {
                    case Direction.Forward:
                        AppTitleManager.This.PopForwarded(_totalBytesForwarded);
                        Interlocked.Add(ref _totalBytesForwarded, bytesRead);
                        break;
                    case Direction.Responding:
                        AppTitleManager.This.PopResponded(_totalBytesResponded);
                        Interlocked.Add(ref _totalBytesResponded, bytesRead);
                        break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}