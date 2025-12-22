using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public interface IServerMessageReceiver<TId>
        where TId : unmanaged, IEquatable<TId>
    {
        public MessageResult HandleDataMessage(TId connectionId, ref DataStreamReader reader);
    }
}
