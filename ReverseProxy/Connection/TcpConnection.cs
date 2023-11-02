using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace ReverseProxy.Connection;

public class TcpConnection
{
  private readonly CancellationTokenSource _cancellationTokenSource = new();
  private readonly IPEndPoint _remoteEndpoint;
  private readonly EndPoint? _serverLocalEndpoint;
  private long _totalBytesForwarded;
  private long _totalBytesResponded;

  private TcpConnection(TcpClient localServerConnection, IPEndPoint remoteEndpoint) {
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

  public static async Task<TcpConnection> AcceptTcpClientAsync(TcpListener tcpListener, IPEndPoint remoteEndpoint) {
    var localServerConnection = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
    localServerConnection.NoDelay = true;
    return new TcpConnection(localServerConnection, remoteEndpoint);
  }

  public void Run(string name) {
    RunInternal(_cancellationTokenSource.Token, name);
  }

  public void Stop(string name) {
    try {
      _cancellationTokenSource.Cancel();
    }
    catch (Exception ex) {
      AppTitleManager.This.PopError();
      Log.Fatal(ex, "TCP {Name} proxy, an exception occurred while closing connection", name);
    }
  }

  private void RunInternal(CancellationToken cancellationToken, string name) {
    Task.Run(async () => {
      try {
        using (Client)
        using (Destination) {
          await Destination.ConnectAsync(_remoteEndpoint.Address, _remoteEndpoint.Port, cancellationToken)
                           .ConfigureAwait(false);
          DestinationEndPoint = Destination.Client.LocalEndPoint;
          Log.Information("TCP {Name} established new connection {ClientEndPoint} => {ServerLocalEndpoint} => {DestinationEndPoint} => {RemoteEndpoint}", name, ClientEndPoint?.ToString(), _serverLocalEndpoint?.ToString(), DestinationEndPoint?.ToString(), _remoteEndpoint?.ToString());

          await using (var serverStream = Destination.GetStream())
          await using (var clientStream = Client.GetStream())
          await using (cancellationToken.Register(() => {
                         serverStream.Close();
                         clientStream.Close();
                       }, true)) {
            await Task.WhenAny(
                               CopyToAsync(clientStream, serverStream, 81920, Direction.Forward, cancellationToken),
                               CopyToAsync(serverStream, clientStream, 81920, Direction.Responding, cancellationToken)
                              ).ConfigureAwait(false);
          }
        }
      }
      catch (Exception ex) {
        AppTitleManager.This.PopError();
        Log.Fatal(ex, "TCP {Name} An exception occurred during TCP stream", name);
      }
      finally {
        AppTitleManager.This.DecreaseLive();
        Log.Information("TCP {Name} connection closed {ClientEndPoint} => {ServerLocalEndpoint} => {DestinationEndPoint} => {RemoteEndpoint}. Forwarded({TotalBytesForwarded})/Responded({TotalBytesResponded}) bytes", name, ClientEndPoint?.ToString(), _serverLocalEndpoint?.ToString(), DestinationEndPoint?.ToString(), _remoteEndpoint?.ToString(), _totalBytesForwarded, _totalBytesResponded);
      }
    });
  }

  private async Task CopyToAsync(Stream source, Stream destination, int bufferSize = 81920,
                                 Direction direction = Direction.Unknown, CancellationToken cancellationToken = default) {
    var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    try {
      while (true) {
        var bytesRead = await source.ReadAsync(new Memory<byte>(buffer), cancellationToken)
                                    .ConfigureAwait(false);
        if (bytesRead == 0) break;
        LastActivity = Environment.TickCount64;
        await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken)
                         .ConfigureAwait(false);
        switch (direction) {
          case Direction.Forward:
            AppTitleManager.This.PopForwarded(_totalBytesForwarded);
            Interlocked.Add(ref _totalBytesForwarded, bytesRead);
            break;
          case Direction.Responding:
            AppTitleManager.This.PopResponded(_totalBytesResponded);
            Interlocked.Add(ref _totalBytesResponded, bytesRead);
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
      }
    }
    finally {
      ArrayPool<byte>.Shared.Return(buffer);
    }
  }
}