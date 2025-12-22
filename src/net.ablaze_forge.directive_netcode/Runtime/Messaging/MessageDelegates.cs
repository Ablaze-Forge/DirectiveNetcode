using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public delegate void ServerMessageDelegate<TId, TMessageMetadata>(TId connectionId, TMessageMetadata metadata, DataStreamReader stream)
        where TId : unmanaged, IEquatable<TId> where TMessageMetadata : unmanaged, IMessageMetadata;

    public delegate void ClientMessageDelegate<TMessageMetadata>(TMessageMetadata metadata, DataStreamReader stream)
        where TMessageMetadata : unmanaged, IMessageMetadata;
}
