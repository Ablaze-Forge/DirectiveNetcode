using AblazeForge.DirectiveNetcode.ConnectionData;
using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public readonly struct DefaultServerMessageReceiver<TId, TMessageMetadata, TConnectionRequirement, TPermissions, TPermissionBaseValue> : IServerMessageReceiver<TId>
        where TId : unmanaged, IEquatable<TId>
        where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamDeserializable<TMessageMetadata>
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TConnectionRequirement : IConnectionPermissionRequirement<TPermissions, TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        private readonly IServerMessageDispatcher<TId, TMessageMetadata, ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> m_DefaultDispatcher;
        private readonly IServerMessageDispatcher<TId, TMessageMetadata, ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> m_VarTrackingDispatcher;
        private readonly IServerMessageDispatcher<TId, TMessageMetadata, ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> m_EventDispatcher;
        private readonly IServerMessageDispatcher<TId, TMessageMetadata, ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> m_ControlDispatcher;
        private readonly IServerMessageDispatcher<TId, TMessageMetadata, ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> m_AuthenticationDispatcher;

        public DefaultServerMessageReceiver(
            IServerMessageDispatcher<TId, TMessageMetadata, ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> defaultDispatcher,
            IServerMessageDispatcher<TId, TMessageMetadata, ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> varTrackingDispatcher,
            IServerMessageDispatcher<TId, TMessageMetadata, ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> eventDispatcher,
            IServerMessageDispatcher<TId, TMessageMetadata, ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> controlDispatcher,
            IServerMessageDispatcher<TId, TMessageMetadata, ServerMessageDelegate<TId, TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> authenticationDispatcher)
        {
            m_DefaultDispatcher = defaultDispatcher;
            m_VarTrackingDispatcher = varTrackingDispatcher;
            m_EventDispatcher = eventDispatcher;
            m_ControlDispatcher = controlDispatcher;
            m_AuthenticationDispatcher = authenticationDispatcher;
        }

        public MessageResult HandleDataMessage(TId connectionId, ref DataStreamReader stream)
        {
            ushort messageId = stream.ReadUShort();
            TMessageMetadata metadata = default(TMessageMetadata).Deserialize(ref stream);

            return metadata.MessageType switch
            {
                MessageType.Default => m_DefaultDispatcher.Dispatch(messageId, connectionId, metadata, ref stream),
                MessageType.VarTracking => m_VarTrackingDispatcher.Dispatch(messageId, connectionId, metadata, ref stream),
                MessageType.Event => m_EventDispatcher.Dispatch(messageId, connectionId, metadata, ref stream),
                MessageType.Control => m_ControlDispatcher.Dispatch(messageId, connectionId, metadata, ref stream),
                MessageType.Authentication => m_AuthenticationDispatcher.Dispatch(messageId, connectionId, metadata, ref stream),

                _ => MessageResult.KeepAlive,
            };
        }
    }
}
