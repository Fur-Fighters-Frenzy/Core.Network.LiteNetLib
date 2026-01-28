#if LITENETLIB_ENABLED
using System;
using System.Net.Sockets;
using Validosik.Core.Network.Transport;
using Validosik.Core.Network.Transport.Interfaces;
using LiteNetLib;

namespace Validosik.Core.Network.LiteNetLib
{
    public sealed class LiteClientAdapter : INetClient, INetEventListener, IDisposable
    {
        public event Action<ReadOnlyMemory<byte>, ChannelKind> OnServerMessage;
        public event Action OnConnectedAsClient;

        private readonly NetManager     _client;
        private readonly LiteChannelMap _map;
        private          NetPeer        _serverPeer;
        private          bool           _disposed;

        public LiteClientAdapter(string host, int port, LiteChannelMap map = null, string connectionKey = "proto")
        {
            _map = map ?? new LiteChannelMap();
            _client = new NetManager(this)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };

            if (!_client.Start())
            {
                throw new InvalidOperationException("LiteNetLib client failed to start.");
            }

            _client.Connect(host, port, connectionKey);
        }

        public void Poll()
        {
            if (_disposed)
            {
                return;
            }

            _client.PollEvents();
        }

        public void Send(ReadOnlySpan<byte> raw, ChannelKind ch = ChannelKind.ReliableOrdered)
        {
            if (_disposed)
            {
                return;
            }

            if (_serverPeer == null)
            {
                return;
            }

            var (delivery, channel) = _map.ToLite(ch);
            _serverPeer.Send(raw, channel, delivery);
        }

        // --- INetEventListener ---
        public void OnPeerConnected(NetPeer peer)
        {
            _serverPeer = peer;
            OnConnectedAsClient?.Invoke();
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (Equals(_serverPeer, peer))
            {
                _serverPeer = null;
            }
        }

        public void OnNetworkError(System.Net.IPEndPoint endPoint, SocketError socketError)
        {
            /* no-op */
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber,
            DeliveryMethod deliveryMethod)
        {
            var data = reader.GetRemainingBytesMemory();
            var kind = _map.FromLite(deliveryMethod, channelNumber);

            OnServerMessage?.Invoke(data, kind);

            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader,
            UnconnectedMessageType messageType)
        {
            reader.Recycle();
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            /* no-op */
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            /* client side not used */
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _client.Stop();
        }
    }
}
#endif