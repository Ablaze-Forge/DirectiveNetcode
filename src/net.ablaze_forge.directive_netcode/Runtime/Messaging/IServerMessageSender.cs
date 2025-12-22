using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public interface IServerMessageSender<TId, TMetadata> 
        where TId : unmanaged, IEquatable<TId>
        where TMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
    {
        public MessagePreparationResult PrepareMessage(TId id, ushort messageId, TMetadata metadata, ref DataStreamWriter writer);
    }
}
