using AblazeForge.DirectiveNetcode.ConnectionData;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public readonly struct DefaultClientMessageReceiver<TMessageMetadata, TConnectionRequirement, TPermissions,TPermissionBaseValue> : IClientMessageReceiver
        where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamDeserializable<TMessageMetadata>
        where TPermissionBaseValue : unmanaged
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TConnectionRequirement : IConnectionPermissionRequirement<TPermissions, TPermissionBaseValue>
    {
        private readonly IClientMessageDispatcher<TMessageMetadata, ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> m_DefaultDispatcher;
        private readonly IClientMessageDispatcher<TMessageMetadata, ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> m_VarTrackingDispatcher;
        private readonly IClientMessageDispatcher<TMessageMetadata, ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> m_EventDispatcher;
        private readonly IClientMessageDispatcher<TMessageMetadata, ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> m_ControlDispatcher;
        private readonly IClientMessageDispatcher<TMessageMetadata, ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> m_AuthenticationDispatcher;

        public DefaultClientMessageReceiver(
            IClientMessageDispatcher<TMessageMetadata, ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> defaultDispatcher,
            IClientMessageDispatcher<TMessageMetadata, ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> varTrackingDispatcher,
            IClientMessageDispatcher<TMessageMetadata, ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> eventDispatcher,
            IClientMessageDispatcher<TMessageMetadata, ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> controlDispatcher,
            IClientMessageDispatcher<TMessageMetadata, ClientMessageDelegate<TMessageMetadata>, TConnectionRequirement, TPermissions, TPermissionBaseValue> authenticationDispatcher)
        {
            m_DefaultDispatcher = defaultDispatcher;
            m_VarTrackingDispatcher = varTrackingDispatcher;
            m_EventDispatcher = eventDispatcher;
            m_ControlDispatcher = controlDispatcher;
            m_AuthenticationDispatcher = authenticationDispatcher;
        }

        public MessageResult HandleDataMessage(ref DataStreamReader stream)
        {
            ushort messageId = stream.ReadUShort();
            TMessageMetadata metadata = default(TMessageMetadata).Deserialize(ref stream);

            return metadata.MessageType switch
            {
                MessageType.Default => m_DefaultDispatcher.Dispatch(messageId, metadata, ref stream),
                MessageType.VarTracking => m_VarTrackingDispatcher.Dispatch(messageId, metadata, ref stream),
                MessageType.Event => m_EventDispatcher.Dispatch(messageId, metadata, ref stream),
                MessageType.Control => m_ControlDispatcher.Dispatch(messageId, metadata, ref stream),
                MessageType.Authentication => m_AuthenticationDispatcher.Dispatch(messageId, metadata, ref stream),

                _ => MessageResult.KeepAlive,
            };
        }
    }
}
