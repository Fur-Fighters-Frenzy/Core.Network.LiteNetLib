#if LITENETLIB_ENABLED
using Validosik.Core.Network.Mapping;
using LiteNetLib;

namespace Validosik.Core.Network.LiteNetLib
{
    internal sealed class LitePlayerMapping
        : PlayerConnectionMapping<
            NetPeer,
            NetPeerPlayerRegistry,
            TokenRegistry> { }

    internal sealed class NetPeerPlayerRegistry
        : BytePlayerRegistry<NetPeer> { }
}
#endif