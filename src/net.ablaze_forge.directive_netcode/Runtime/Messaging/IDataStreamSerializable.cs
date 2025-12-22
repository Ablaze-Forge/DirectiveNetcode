using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public interface IDataStreamSerializable
    {
        public bool Serialize(ref DataStreamWriter writer);
    }
}
