# Core.Network.LiteNetLib

LiteNetLib adapter for **Core.Network**.

This package bridges **LiteNetLib** (`NetManager`, `NetPeer`, `DeliveryMethod`) to the `Core.Network` transport contracts (`INetClient` / `INetServer`) and unified channels (`ChannelKind`). It lets higher-level networking code stay transport-agnostic while using LiteNetLib as the runtime transport.

- [Core.Network](https://github.com/Fur-Fighters-Frenzy/Core.Network)
- [LiteNetLib](https://github.com/RevenantX/LiteNetLib)

> **Status:** WIP

---

## What’s included

- `LiteClientAdapter`: `INetClient` implementation backed by `NetManager` (client mode)
- `LiteServerAdapter`: `INetServer` implementation backed by `NetManager` (server mode)
- `LiteChannelMap`: maps `ChannelKind` <-> `(DeliveryMethod, channelNumber)`
- Connection → `PlayerId` mapping with a minimal handshake based on `HandshakeDto`

---

## Notes

- LiteNetLib requires polling: call `Poll()` regularly on both client and server adapters.
- `ChannelKind` mapping is configurable via `LiteChannelMap.Set(...)`.

---

## Scripting define symbol

This adapter is compiled only when:

- `LITENETLIB_ENABLED`

---

# Part of the Core Project

This package is part of the **Core** project, which consists of multiple Unity packages.
See the full project here: [Core](https://github.com/Fur-Fighters-Frenzy/Core)

---