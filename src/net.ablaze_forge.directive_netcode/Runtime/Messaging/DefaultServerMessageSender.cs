using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public readonly struct DefaultServerMessageSender<TId, TMetadata> : IServerMessageSender<TId, TMetadata>
        where TId : unmanaged, IEquatable<TId>
        where TMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
    {
        public MessagePreparationResult PrepareMessage(TId id, ushort messageId, TMetadata metadata, ref DataStreamWriter writer)
        {
            try
            {
                writer.WriteUShort(messageId);
                metadata.Serialize(ref writer);

                return MessagePreparationResult.Success;
            }
            catch
            {
                return MessagePreparationResult.Errored;
            }
        }
    }
}
