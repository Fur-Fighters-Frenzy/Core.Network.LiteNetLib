#if LITENETLIB_ENABLED
using System.Collections.Generic;
using Validosik.Core.Network.Transport;
using LiteNetLib;

namespace Validosik.Core.Network.LiteNetLib
{
    public sealed class LiteChannelMap
    {
        // forward: our ChannelKind -> (Delivery, channelNumber)
        private readonly Dictionary<ChannelKind, (DeliveryMethod delivery, byte channel)> _fwd =
            new()
            {
                [ChannelKind.ReliableOrdered] = (DeliveryMethod.ReliableOrdered, 0),
                [ChannelKind.Unreliable] = (DeliveryMethod.Unreliable, 1),
                [ChannelKind.Sequenced] = (DeliveryMethod.Sequenced, 2),
            };

        // reverse: (Delivery, channelNumber) -> our ChannelKind
        private readonly Dictionary<(DeliveryMethod, byte), ChannelKind> _rev =
            new()
            {
                [(DeliveryMethod.ReliableOrdered, 0)] = ChannelKind.ReliableOrdered,
                [(DeliveryMethod.Unreliable, 1)] = ChannelKind.Unreliable,
                [(DeliveryMethod.Sequenced, 2)] = ChannelKind.Sequenced,
            };

        public void Set(ChannelKind kind, DeliveryMethod delivery, byte channel)
        {
            _fwd[kind] = (delivery, channel);
            _rev[(delivery, channel)] = kind;
        }

        public (DeliveryMethod delivery, byte channel) ToLite(ChannelKind kind)
            => _fwd.TryGetValue(kind, out var v) ? v : (DeliveryMethod.ReliableOrdered, (byte)0);

        public ChannelKind FromLite(DeliveryMethod delivery, byte channel)
            => _rev.TryGetValue((delivery, channel), out var k) ? k : ChannelKind.ReliableOrdered;
    }
}
#endif