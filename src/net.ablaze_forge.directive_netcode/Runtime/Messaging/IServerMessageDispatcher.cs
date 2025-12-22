using AblazeForge.DirectiveNetcode.ConnectionData;
using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public interface IServerMessageDispatcher<TId, TMessageMetadata, TMessageDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>
        where TId : unmanaged, IEquatable<TId>
        where TMessageDelegate : Delegate
        where TMessageMetadata : unmanaged, IMessageMetadata
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TConnectionRequirement : IConnectionPermissionRequirement<TPermissions, TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        public MessageResult Dispatch(ushort messageKey, TId connectionId, TMessageMetadata messageMetadata, ref DataStreamReader reader);
    }
}
