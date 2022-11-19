using System.Buffers;
using System.Net.Sockets;
using System.Net;
using MoonReverseProxy;
using System.Xml.Linq;

namespace ReverseProxy_NET6.Proxy
{
    public class TcpConnection
    {
        private static readonly EasLog logger = IEasLog.CreateLogger("TcpConnection");

        public TcpClient Client
        {
            get
            {
                return _localServerConnection;
            }
        }
        public TcpClient Destination
        {
            get
            {
                return _forwardClient;
            }
        }
        public EndPoint? ClientEndPoint
        {
            get
            {
                return _sourceEndpoint;
            }
        }
        public EndPoint? DestinationEndPoint
        {
            get
            {
                return _forwardLocalEndpoint;
            }
        }

        public long TotalBytesForwarded
        {
            get
            {
                return _totalBytesForwarded;
            }
        }
        public long TotalBytesResponded
        {
            get
            {
                return _totalBytesResponded;
            }
        }
        public DateTime ConnectedAt { get; private set; }
        private readonly TcpClient _localServerConnection;
        private readonly EndPoint? _sourceEndpoint;
        private readonly IPEndPoint _remoteEndpoint;
        private readonly TcpClient _forwardClient;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly EndPoint? _serverLocalEndpoint;
        private EndPoint? _forwardLocalEndpoint;
        private long _totalBytesForwarded;
        private long _totalBytesResponded;
        public long LastActivity { get; private set; } = Environment.TickCount64;

        public static async Task<TcpConnection> AcceptTcpClientAsync(TcpListener tcpListener, IPEndPoint remoteEndpoint)
        {
            var localServerConnection = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
            localServerConnection.NoDelay = true;
            return new TcpConnection(localServerConnection, remoteEndpoint);
        }

        private TcpConnection(TcpClient localServerConnection, IPEndPoint remoteEndpoint)
        {
            ConnectedAt = DateTime.Now;
            _localServerConnection = localServerConnection;
            _remoteEndpoint = remoteEndpoint;

            _forwardClient = new TcpClient { NoDelay = true };

            _sourceEndpoint = _localServerConnection.Client.RemoteEndPoint;
            _serverLocalEndpoint = _localServerConnection.Client.LocalEndPoint;
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
                logger.Exception(ex, "TCP", name, $"An exception occurred while closing TcpConnection.");
            }
        }

        private void RunInternal(CancellationToken cancellationToken, string name)
        {
            Task.Run(async () =>
            {
                try
                {
                    using (_localServerConnection)
                    using (_forwardClient)
                    {
                        await _forwardClient.ConnectAsync(_remoteEndpoint.Address, _remoteEndpoint.Port, cancellationToken).ConfigureAwait(false);
                        _forwardLocalEndpoint = _forwardClient.Client.LocalEndPoint;
                        logger.Info("TCP", name, "ESTABLISHED", $"{_sourceEndpoint} => {_serverLocalEndpoint} => {_forwardLocalEndpoint} => {_remoteEndpoint}");

                        using (var serverStream = _forwardClient.GetStream())
                        using (var clientStream = _localServerConnection.GetStream())
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
                    logger.Exception(ex, "TCP", name, $"An exception occurred during TCP stream.");
                }
                finally
                {
                    logger.Warn("TCP", name, $"Connection {_sourceEndpoint} => {_serverLocalEndpoint} => {_forwardLocalEndpoint} => {_remoteEndpoint}. Forwarded({_totalBytesForwarded})/Responded({_totalBytesResponded}) bytes.");
                }
            });
        }

        private async Task CopyToAsync(Stream source, Stream destination, int bufferSize = 81920, Direction direction = Direction.Unknown, CancellationToken cancellationToken = default)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                while (true)
                {
                    int bytesRead = await source.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0) break;
                    LastActivity = Environment.TickCount64;
                    await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);

                    switch (direction)
                    {
                        case Direction.Forward:
                            Interlocked.Add(ref _totalBytesForwarded, bytesRead);
                            break;
                        case Direction.Responding:
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


}
