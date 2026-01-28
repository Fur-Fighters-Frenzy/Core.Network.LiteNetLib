#if LITENETLIB_ENABLED
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Validosik.Core.Network.Dto;
using Validosik.Core.Network.Transport;
using Validosik.Core.Network.Transport.Interfaces;
using Validosik.Core.Network.Types;
using LiteNetLib;

namespace Validosik.Core.Network.LiteNetLib
{
    public sealed class LiteServerAdapter : INetServer, INetEventListener, IDisposable
    {
        public event Action<PlayerId, ReadOnlyMemory<byte>, ChannelKind> OnClientMessage;
        public event Action<PlayerId> OnClientDisconnected;
        public event Action<PlayerId> OnClientConnected;

        private readonly NetManager     _server;
        private readonly LiteChannelMap _map;
        private readonly string         _connectionKey;
        private          bool           _disposed;

        private LitePlayerMapping _registry;

        public LiteServerAdapter(int port, LiteChannelMap map = null, string connectionKey = "proto")
        {
            _map = map ?? new LiteChannelMap();
            _server = new NetManager(this)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };

            if (!_server.Start(port))
            {
                throw new InvalidOperationException($"LiteNetLib server failed to start on port {port}.");
            }

            _connectionKey = connectionKey;
            _registry = new LitePlayerMapping();
        }

        public void Send(PlayerId to, ReadOnlySpan<byte> raw, ChannelKind ch = ChannelKind.ReliableOrdered)
        {
            if (_disposed)
            {
                return;
            }

            if (!_registry.TryGetConnection(to, out var peer))
            {
                return;
            }

            var (delivery, channel) = _map.ToLite(ch);
            peer.Send(raw, channel, delivery);
        }

        public void Broadcast(ReadOnlySpan<byte> raw, ChannelKind ch = ChannelKind.ReliableOrdered)
        {
            if (_disposed)
            {
                return;
            }

            var (delivery, channel) = _map.ToLite(ch);
            foreach (var (peer, _) in _registry.AllConnections)
            {
                peer.Send(raw, channel, delivery);
            }
        }

        public void BroadcastExcept(PlayerId except, ReadOnlySpan<byte> raw,
            ChannelKind ch = ChannelKind.ReliableOrdered)
        {
            if (_disposed)
            {
                return;
            }

            var (delivery, channel) = _map.ToLite(ch);

            foreach (var (peer, pid) in _registry.AllConnections)
            {
                if (pid == except.Value)
                {
                    continue;
                }

                peer.Send(raw, channel, delivery);
            }
        }

        public void BroadcastExcept(PlayerId[] except, ReadOnlySpan<byte> raw,
            ChannelKind ch = ChannelKind.ReliableOrdered)
        {
            if (_disposed)
            {
                return;
            }

            if (except == null || except.Length == 0)
            {
                Broadcast(raw, ch);
                return;
            }

            var skip = new HashSet<byte>();
            foreach (var pid in except)
            {
                skip.Add(pid.Value);
            }

            var (delivery, channel) = _map.ToLite(ch);

            foreach (var (peer, pid) in _registry.AllConnections)
            {
                if (skip.Contains(pid))
                {
                    continue;
                }

                peer.Send(raw, channel, delivery);
            }
        }

        public void Poll()
        {
            if (_disposed)
            {
                return;
            }

            _server.PollEvents();
        }

        public void Disconnect(PlayerId pid)
        {
            if (_registry.TryGetConnection(pid, out var peer))
            {
                peer.Disconnect();
            }
        }

        // --- INetEventListener ---
        public void OnPeerConnected(NetPeer peer)
        {
            var (pid, token) = _registry.MapConnectionToPlayer(peer);
            // OnClientConnected moved from here to Handshake sending logic 
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            var pid = _registry.ReleaseConnection(peer);
            OnClientDisconnected?.Invoke(pid);
        }

        public void OnNetworkError(System.Net.IPEndPoint endPoint, SocketError socketError)
        {
            /* no-op */
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber,
            DeliveryMethod deliveryMethod)
        {
            var data = reader.GetRemainingBytesMemory();

            if (!_registry.TryGetPid(peer, out var pid))
            {
                if (!HandshakeDto.TryFromBytes(data.Span, out var handshakeDto))
                {
                    reader.Recycle();
                    return;
                }

                var (playerId, token) = _registry.MapConnectionToPlayer(peer);
                pid = playerId;

                var handshake = new HandshakeDto(
                    0,
                    0,
                    pid,
                    token
                );

                Span<byte> handshakeBytes = stackalloc byte[HandshakeDto.Size];
                if (!handshake.TryWrite(handshakeBytes, out var hw) || hw != HandshakeDto.Size)
                {
                    reader.Recycle();
                    return;
                }

                // byte[]: [u16 msgType][u16 payloadLen][payload...]
                // Pack handshake into NetEnvelope without allocations (31 bytes total here).
                Span<byte> envelope = stackalloc byte[4 + HandshakeDto.Size];
                envelope[0] = (byte)ServerMsgType.Handshake;
                envelope[1] = 0;
                envelope[2] = (byte)HandshakeDto.Size;
                envelope[3] = 0;
                handshakeBytes.CopyTo(envelope.Slice(4));

                Send(pid, envelope);

                OnClientConnected?.Invoke(pid);

                reader.Recycle();
                return;
            }

            var kind = _map.FromLite(deliveryMethod, channelNumber);

            OnClientMessage?.Invoke(pid, data, kind);

            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader,
            UnconnectedMessageType messageType)
        {
            reader.Recycle();
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            /* client side not used */
        }

        public void OnConnectionRequest(ConnectionRequest request) =>
            request.AcceptIfKey(_connectionKey);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _server.Stop();
            _registry = null;
        }

        public ushort Tick { get; private set; }

        public void NextTick()
        {
            unchecked
            {
                ++Tick;
            }
        }
    }
}
#endif