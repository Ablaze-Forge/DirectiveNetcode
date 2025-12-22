using AblazeForge.DirectiveNetcode.ConnectionData;
using System.Collections.Concurrent;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public class GenericClientMessageDispatcher<TMessageMetadata, TConnectionRequirement, TPermissions, TPermissionBaseValue> : IClientMessageDispatcher<TMessageMetadata, ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue>, IMessageRegistrar<ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue>
        where TMessageMetadata : unmanaged, IMessageMetadata
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TConnectionRequirement : IConnectionPermissionRequirement<TPermissions, TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        public ConcurrentDictionary<ushort, MessageDelegateInfo<ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue>> MessageDelegates { get; } = new();

        public MessageResult Dispatch(ushort messageKey, TMessageMetadata messageMetadata, ref DataStreamReader reader)
        {
            // TODO: Add permission provider
            if (MessageDelegates.TryGetValue(messageKey, out var messageDelegateInfo))
            {
                messageDelegateInfo.Delegate.Invoke(messageMetadata, reader);

                return MessageResult.Success;
            }
            else
            {
                return MessageResult.KeepAlive;
            }
        }
    }
}
