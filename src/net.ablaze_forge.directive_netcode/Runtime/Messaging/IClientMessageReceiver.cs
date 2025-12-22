using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public interface IClientMessageReceiver
    {
        public MessageResult HandleDataMessage(ref DataStreamReader stream);
    }
}
