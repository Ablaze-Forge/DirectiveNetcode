using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public interface IDataStreamDeserializable<T> where T : unmanaged, IMessageMetadata
    {
        public T Deserialize(ref DataStreamReader reader);
    }
}
