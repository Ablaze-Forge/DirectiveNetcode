using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public readonly struct DefaultClientMessageSender<TMetadata> : IClientMessageSender<TMetadata>
        where TMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
    {
        public MessagePreparationResult PrepareMessage(ushort messageId, TMetadata metadata, ref DataStreamWriter writer)
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
