using AblazeForge.DirectiveNetcode.ConnectionData;
using System;
using System.Collections.Concurrent;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public class GenericServerMessageDispatcher<TId, TMessageMetadata, TConnectionRequirement, TPermissions, TPermissionBaseValue>
        : IServerMessageDispatcher<TId, TMessageMetadata, ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue>,
        IMessageRegistrar<ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue>
        where TId : unmanaged, IEquatable<TId>
        where TMessageMetadata : unmanaged, IMessageMetadata
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TConnectionRequirement : IConnectionPermissionRequirement<TPermissions, TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        public ConcurrentDictionary<ushort, MessageDelegateInfo<ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue>> MessageDelegates { get; } = new();

        public MessageResult Dispatch(ushort messageKey, TId connectionId, TMessageMetadata messageMetadata, ref DataStreamReader reader)
        {
            // TODO: Add permission provider
            if (MessageDelegates.TryGetValue(messageKey, out var messageDelegateInfo))
            {
                messageDelegateInfo.Delegate.Invoke(connectionId, messageMetadata, reader);

                return MessageResult.Success;
            }
            else
            {
                return MessageResult.KeepAlive;
            }
        }
    }
}
