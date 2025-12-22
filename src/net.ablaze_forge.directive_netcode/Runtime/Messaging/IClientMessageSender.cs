using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public interface IClientMessageSender<TMetadata>
        where TMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
    {
        public MessagePreparationResult PrepareMessage(ushort messageId, TMetadata metadata, ref DataStreamWriter writer);
    }
}
