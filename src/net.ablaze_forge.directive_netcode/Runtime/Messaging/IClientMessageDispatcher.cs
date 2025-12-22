using AblazeForge.DirectiveNetcode.ConnectionData;
using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public interface IClientMessageDispatcher<TMessageMetadata, TMessageDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>
        where TMessageMetadata : unmanaged, IMessageMetadata
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TConnectionRequirement : IConnectionPermissionRequirement<TPermissions, TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        public MessageResult Dispatch(ushort messageKey, TMessageMetadata messageMetadata, ref DataStreamReader reader);
    }
}
