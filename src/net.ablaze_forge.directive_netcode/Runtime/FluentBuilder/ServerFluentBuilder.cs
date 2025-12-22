using AblazeForge.DirectiveNetcode.ConnectionData;
using AblazeForge.DirectiveNetcode.ConnectionIdProviders;
using AblazeForge.DirectiveNetcode.Engines;
using AblazeForge.DirectiveNetcode.Logging;
using AblazeForge.DirectiveNetcode.Messaging;
using System;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.FluentBuilders
{
    public interface IBuildStep<T>
    {
        T Build();
    }

    public interface IOptionalLoggerStep<T>
    {
        public IDirectiveNetcodeLogger Logger { get; set; }
        T UseLogger(IDirectiveNetcodeLogger logger);
    }

    public class ServerConnectionManagerBuilder
    {
        public IConnectionIdStep<TId> WithConnectionIdProvider<TId>(IConnectionIdProvider<TId> connectionIdProvider)
            where TId : unmanaged, IEquatable<TId>
        {
            return new ConnectionIdStep<TId>(connectionIdProvider);
        }

        public IConnectionIdStep<int> WithIntConnectionIdProvider(out IntConnectionIdProvider connectionIdProvider)
        {
            connectionIdProvider = new IntConnectionIdProvider();
            return new ConnectionIdStep<int>(connectionIdProvider);
        }

        public interface IConnectionIdStep<TId> where TId : unmanaged, IEquatable<TId>
        {
            IConnectionFilterStep<TId> WithConnectionFilter(IConnectionFilter filter);
            IConnectionFilterStep<TId> WithoutConnectionFilter();
        }

        private class ConnectionIdStep<TId> : IConnectionIdStep<TId> where TId : unmanaged, IEquatable<TId>
        {
            private readonly IConnectionIdProvider<TId> m_ConnectionIdProvider;
            public ConnectionIdStep(IConnectionIdProvider<TId> connectionIdProvider) => m_ConnectionIdProvider = connectionIdProvider;

            public IConnectionFilterStep<TId> WithConnectionFilter(IConnectionFilter filter) => new ConnectionFilterStep<TId>(m_ConnectionIdProvider, filter);
            public IConnectionFilterStep<TId> WithoutConnectionFilter() => new ConnectionFilterStep<TId>(m_ConnectionIdProvider, new NoOpConnectionFilter());
        }

        public interface IConnectionFilterStep<TId> where TId : unmanaged, IEquatable<TId>
        {
            IConnectionInfoTypeStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
                WithConnectionInformationType<TPermissions, TConnectionInformation, TPermissionBaseValue>()
                    where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
                    where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
                    where TPermissionBaseValue : unmanaged;
        }

        private class ConnectionFilterStep<TId> : IConnectionFilterStep<TId> where TId : unmanaged, IEquatable<TId>
        {
            private readonly IConnectionIdProvider<TId> m_ConnectionIdProvider;
            private readonly IConnectionFilter m_ConnectionFilter;

            public ConnectionFilterStep(IConnectionIdProvider<TId> connectionIdProvider, IConnectionFilter connectionFilter)
            {
                m_ConnectionIdProvider = connectionIdProvider;
                m_ConnectionFilter = connectionFilter;
            }

            public IConnectionInfoTypeStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
                WithConnectionInformationType<TPermissions, TConnectionInformation, TPermissionBaseValue>()
                    where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
                    where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
                    where TPermissionBaseValue : unmanaged
            {
                return new ConnectionInfoTypeStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>(m_ConnectionIdProvider, m_ConnectionFilter);
            }
        }

        public interface IConnectionInfoTypeStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
            : IBuildStep<DefaultServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>>
                where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
                where TId : unmanaged, IEquatable<TId>
                where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
                where TPermissionBaseValue : unmanaged
        { }

        private class ConnectionInfoTypeStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> : IConnectionInfoTypeStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
            where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
            where TId : unmanaged, IEquatable<TId>
            where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
            where TPermissionBaseValue : unmanaged
        {
            private readonly IConnectionIdProvider<TId> m_ConnectionIdProvider;
            private readonly IConnectionFilter m_ConnectionFilter;

            public ConnectionInfoTypeStep(IConnectionIdProvider<TId> connectionIdProvider, IConnectionFilter connectionFilter)
            {
                m_ConnectionIdProvider = connectionIdProvider;
                m_ConnectionFilter = connectionFilter;
            }

            public DefaultServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> Build()
            {
                return new DefaultServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>(m_ConnectionIdProvider, m_ConnectionFilter);
            }
        }
    }

    public class ServerMessageManagerBuilder
    {
        public ISynchronizatorStep WithSynchronizator(IServerContextSynchronizator contextSynchronizator) => new SynchronizatorStep(contextSynchronizator);

        public ISynchronizatorStep WithDefaultSynchronizator(out DefaultServerContextSynchronizator synchronizator)
        {
            synchronizator = new DefaultServerContextSynchronizator();
            return new SynchronizatorStep(synchronizator);
        }

        public interface ISynchronizatorStep
        {
            public IConnectionManagerStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
                WithConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>(IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> connectionManager)
                    where TId : unmanaged, IEquatable<TId>
                    where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
                    where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
                    where TPermissionBaseValue : unmanaged;
        }

        public class SynchronizatorStep : ISynchronizatorStep
        {
            private readonly IServerContextSynchronizator m_Synchronizator;
            public SynchronizatorStep(IServerContextSynchronizator synchronizator) => m_Synchronizator = synchronizator;

            public IConnectionManagerStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
            WithConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>(IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> connectionManager)
                where TId : unmanaged, IEquatable<TId>
                where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
                where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
                where TPermissionBaseValue : unmanaged
            {
                return new ConnectionManagerStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>(m_Synchronizator, connectionManager);
            }
        }

        public interface IConnectionManagerStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
            where TId : unmanaged, IEquatable<TId>
            where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
            where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
            where TPermissionBaseValue : unmanaged
        {
            public IMessageSenderStep<TId, TPermissions, TConnectionInformation, TMessageMetadata, TPermissionBaseValue>
                UseMessageSender<TMessageMetadata>(IServerMessageSender<TId, TMessageMetadata> messageSender)
                    where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable;
        }

        public class ConnectionManagerStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> : IConnectionManagerStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
            where TId : unmanaged, IEquatable<TId>
            where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
            where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
            where TPermissionBaseValue : unmanaged
        {
            private readonly IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> m_ConnectionManager;
            private readonly IServerContextSynchronizator m_Synchronizator;

            public ConnectionManagerStep(IServerContextSynchronizator synchronizator, IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> connectionManager)
            {
                m_ConnectionManager = connectionManager;
                m_Synchronizator = synchronizator;
            }

            public IMessageSenderStep<TId, TPermissions, TConnectionInformation, TMessageMetadata, TPermissionBaseValue>
                UseMessageSender<TMessageMetadata>(IServerMessageSender<TId, TMessageMetadata> messageSender)
                    where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
            {
                return new MessageSenderStep<TId, TPermissions, TConnectionInformation, TMessageMetadata, TPermissionBaseValue>(m_Synchronizator, m_ConnectionManager, messageSender);
            }
        }

        public interface IMessageSenderStep<TId, TPermissions, TConnectionInformation, TMessageMetadata, TPermissionBaseValue>
            where TId : unmanaged, IEquatable<TId>
            where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
            where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
            where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
            where TPermissionBaseValue : unmanaged
        {
            public ServerMessageManager<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue> Build();
        }

        public class MessageSenderStep<TId, TPermissions, TConnectionInformation, TMessageMetadata, TPermissionBaseValue>
            : IMessageSenderStep<TId, TPermissions, TConnectionInformation, TMessageMetadata, TPermissionBaseValue>
                where TId : unmanaged, IEquatable<TId>
                where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
                where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
                where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
                where TPermissionBaseValue : unmanaged
        {
            private readonly IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> m_ConnectionManager;
            private readonly IServerContextSynchronizator m_Synchronizator;
            private readonly IServerMessageSender<TId, TMessageMetadata> m_MessageSender;

            public MessageSenderStep(IServerContextSynchronizator synchronizator, IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> connectionManager, IServerMessageSender<TId, TMessageMetadata> messageSender)
            {
                m_ConnectionManager = connectionManager;
                m_Synchronizator = synchronizator;
                m_MessageSender = messageSender;
            }

            public ServerMessageManager<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue> Build()
            {
                return new ServerMessageManager<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>(m_Synchronizator, m_ConnectionManager, m_MessageSender);
            }
        }
    }

    public class ServerEngineBuilder
    {
        public ISynchronizatorStep WithSynchronizator(IServerContextSynchronizator contextSynchronizator) => new SynchronizatorStep(contextSynchronizator);

        public ISynchronizatorStep WithDefaultSynchronizator(out DefaultServerContextSynchronizator synchronizator)
        {
            synchronizator = new DefaultServerContextSynchronizator();
            return new SynchronizatorStep(synchronizator);
        }

        public interface ISynchronizatorStep
        {
            IUpdateTypeStep WithUpdateType(Type updateType);
        }

        private class SynchronizatorStep : ISynchronizatorStep
        {
            private readonly IServerContextSynchronizator m_ContextSynchronizator;
            public SynchronizatorStep(IServerContextSynchronizator contextSynchronizator) => m_ContextSynchronizator = contextSynchronizator;
            public IUpdateTypeStep WithUpdateType(Type updateType) => new UpdateTypeStep(m_ContextSynchronizator, updateType);
        }

        public interface IUpdateTypeStep
        {
            IConnectionManagerDefinitionStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
                UseConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>(IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> connectionManager)
                    where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
                    where TId : unmanaged, IEquatable<TId>
                    where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
                    where TPermissionBaseValue : unmanaged;
        }

        private class UpdateTypeStep : IUpdateTypeStep
        {
            private readonly IServerContextSynchronizator m_ContextSynchronizator;
            private readonly Type m_UpdateType;

            public UpdateTypeStep(IServerContextSynchronizator contextSynchronizator, Type updateType)
            {
                m_ContextSynchronizator = contextSynchronizator;
                m_UpdateType = updateType;
            }

            public IConnectionManagerDefinitionStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
                UseConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>(IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> connectionManager)
                    where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
                    where TId : unmanaged, IEquatable<TId>
                    where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
                    where TPermissionBaseValue : unmanaged
            {
                return new ConnectionManagerDefinitionStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>(m_ContextSynchronizator, connectionManager, m_UpdateType);
            }
        }

        public interface IConnectionManagerDefinitionStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
            where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
            where TId : unmanaged, IEquatable<TId>
            where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
            where TPermissionBaseValue : unmanaged
        {
            IMessageManagerStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>
                WithMessageManager<TMessageMetadata>(IServerMessageManager<TId, TMessageMetadata> messageManager)
                    where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable;
        }

        private class ConnectionManagerDefinitionStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> : IConnectionManagerDefinitionStep<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
            where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
            where TId : unmanaged, IEquatable<TId>
            where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
            where TPermissionBaseValue : unmanaged
        {
            private readonly IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> m_ConnectionManager;
            private readonly IServerContextSynchronizator m_ContextSynchronizator;
            private readonly Type m_UpdateType;

            public ConnectionManagerDefinitionStep(IServerContextSynchronizator contextSynchronizator, IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> connectionManager, Type updateType)
            {
                m_ContextSynchronizator = contextSynchronizator;
                m_ConnectionManager = connectionManager;
                m_UpdateType = updateType;
            }

            public IMessageManagerStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>
                WithMessageManager<TMessageMetadata>(IServerMessageManager<TId, TMessageMetadata> messageManager)
                   where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
            {
                return new MessageManagerStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>(m_ContextSynchronizator, m_ConnectionManager, messageManager, m_UpdateType);
            }
        }

        public interface IMessageManagerStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>
            where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
            where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
            where TId : unmanaged, IEquatable<TId>
            where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
            where TPermissionBaseValue : unmanaged
        {
            IMessageReceiverStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>
                WithMessageReceiver(IServerMessageReceiver<TId> messageReceiver);
        }

        private class MessageManagerStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>
            : IMessageManagerStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>
                where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
                where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
                where TId : unmanaged, IEquatable<TId>
                where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
                where TPermissionBaseValue : unmanaged
        {
            private readonly IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> m_ConnectionManager;
            private readonly IServerMessageManager<TId, TMessageMetadata> m_MessageManager;
            private readonly IServerContextSynchronizator m_ContextSynchronizator;
            private readonly Type m_UpdateType;

            public MessageManagerStep(IServerContextSynchronizator contextSynchronizator, IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> connectionManager, IServerMessageManager<TId, TMessageMetadata> messageManager, Type updateType)
            {
                m_ContextSynchronizator = contextSynchronizator;
                m_ConnectionManager = connectionManager;
                m_MessageManager = messageManager;
                m_UpdateType = updateType;
            }

            public IMessageReceiverStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue> WithMessageReceiver(IServerMessageReceiver<TId> messageReceiver)
            {
                return new MessageReceiverStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>(m_ContextSynchronizator, m_ConnectionManager, m_MessageManager, messageReceiver, m_UpdateType);
            }
        }

        public interface IMessageReceiverStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>
            : IBuildStep<ServerEngine<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>>,
              IOptionalLoggerStep<IMessageReceiverStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>>
                where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
                where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
                where TId : unmanaged, IEquatable<TId>
                where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
                where TPermissionBaseValue : unmanaged
        { }

        private class MessageReceiverStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>
            : IMessageReceiverStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>
                where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
                where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
                where TId : unmanaged, IEquatable<TId>
                where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
                where TPermissionBaseValue : unmanaged
        {
            public IDirectiveNetcodeLogger Logger { get; set; }

            private readonly IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> m_ConnectionManager;
            private readonly IServerMessageManager<TId, TMessageMetadata> m_MessageManager;
            private readonly IServerMessageReceiver<TId> m_MessageReceiver;
            private readonly IServerContextSynchronizator m_ContextSynchronizator;
            private readonly Type m_UpdateType;

            public MessageReceiverStep(
                IServerContextSynchronizator contextSynchronizator,
                IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> connectionManager,
                IServerMessageManager<TId, TMessageMetadata> messageManager,
                IServerMessageReceiver<TId> messageReceiver,
                Type updateType)
            {
                m_ContextSynchronizator = contextSynchronizator;
                m_ConnectionManager = connectionManager;
                m_MessageManager = messageManager;
                m_MessageReceiver = messageReceiver;
                m_UpdateType = updateType;
                Logger = new DirectiveNetcodeLogger(Debug.unityLogger);
            }

            public IMessageReceiverStep<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue> UseLogger(IDirectiveNetcodeLogger logger)
            {
                Logger = logger;
                return this;
            }

            public ServerEngine<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue> Build()
            {
                return new ServerEngine<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue>(
                    Logger ?? new DirectiveNetcodeLogger(Debug.unityLogger),
                    m_ContextSynchronizator,
                    m_ConnectionManager,
                    m_MessageManager,
                    m_MessageReceiver,
                    m_UpdateType);
            }
        }
    }
}
