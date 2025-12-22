using AblazeForge.DirectiveNetcode.Engines;
using AblazeForge.DirectiveNetcode.Logging;
using AblazeForge.DirectiveNetcode.Messaging;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.FluentBuilders
{
    public class ClientMessageManagerBuilder<TMetadata>
        where TMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
    {
        public ISynchronizatorStep WithContextSynchronizator(IClientContextSynchronizator contextSynchronizator)
        {
            return new SynchronizatorStep(contextSynchronizator);
        }

        public ISynchronizatorStep WithDefaultContextSynchronizator(out IClientContextSynchronizator contextSynchronizator)
        {
            contextSynchronizator = new DefaultClientContextSynchronizator();

            return new SynchronizatorStep(contextSynchronizator);
        }

        public interface ISynchronizatorStep
        {
            IMessageSenderStep UseMessageSender(IClientMessageSender<TMetadata> messageSender);
        }

        private class SynchronizatorStep : ISynchronizatorStep
        {
            private readonly IClientContextSynchronizator m_Synchronizator;

            public SynchronizatorStep(IClientContextSynchronizator synchronizator)
            {
                m_Synchronizator = synchronizator;
            }

            public IMessageSenderStep UseMessageSender(IClientMessageSender<TMetadata> messageSender)
            {
                return new MessageSenderStep(m_Synchronizator, messageSender);
            }
        }

        public interface IMessageSenderStep : IBuildStep<ClientMessageManager<TMetadata>>
        {
        }

        private class MessageSenderStep : IMessageSenderStep
        {
            private readonly IClientContextSynchronizator m_Synchronizator;
            private readonly IClientMessageSender<TMetadata> m_MessageSender;

            public MessageSenderStep(IClientContextSynchronizator synchronizator, IClientMessageSender<TMetadata> messageSender)
            {
                m_Synchronizator = synchronizator;
                m_MessageSender = messageSender;
            }

            public ClientMessageManager<TMetadata> Build()
            {
                return new ClientMessageManager<TMetadata>(m_Synchronizator, m_MessageSender);
            }
        }
    }

    public class ClientEngineBuilder
    {
        public IClientContextSynchronizatorStep WithContextSynchronizator(IClientContextSynchronizator contextSynchronizator)
        {
            return new ClientContextSynzhronizatorStep(contextSynchronizator);
        }

        public IClientContextSynchronizatorStep WithDefaultContextSynchronizator(out IClientContextSynchronizator contextSynchronizator)
        {
            contextSynchronizator = new DefaultClientContextSynchronizator();

            return new ClientContextSynzhronizatorStep(contextSynchronizator);
        }

        public interface IClientContextSynchronizatorStep
        {
            IMessageManagerStep<TMessageMetadata> WithMessageManager<TMessageMetadata>(IClientMessageManager<TMessageMetadata> messageManager)
                where TMessageMetadata : unmanaged, IMessageMetadata;
        }

        private class ClientContextSynzhronizatorStep : IClientContextSynchronizatorStep
        {
            private readonly IClientContextSynchronizator m_ContextSynchronizator;

            public ClientContextSynzhronizatorStep(IClientContextSynchronizator contextSynchronizator)
            {
                m_ContextSynchronizator = contextSynchronizator;
            }

            public IMessageManagerStep<TMessageMetadata> WithMessageManager<TMessageMetadata>(IClientMessageManager<TMessageMetadata> messageManager)
                where TMessageMetadata : unmanaged, IMessageMetadata
            {
                return new MessageManagerStep<TMessageMetadata>(m_ContextSynchronizator, messageManager);
            }
        }

        public interface IMessageManagerStep<TMessageMetadata>
            where TMessageMetadata : unmanaged, IMessageMetadata
        {
            IMessageReceiverStep<TMessageMetadata> WithMessageReceiver(IClientMessageReceiver messageReceiver);
        }

        private class MessageManagerStep<TMessageMetadata> : IMessageManagerStep<TMessageMetadata>
            where TMessageMetadata : unmanaged, IMessageMetadata
        {
            private readonly IClientContextSynchronizator m_ContextSynchronizator;
            private readonly IClientMessageManager<TMessageMetadata> m_MessageManager;

            public MessageManagerStep(IClientContextSynchronizator contextSynchronizator, IClientMessageManager<TMessageMetadata> messageManager)
            {
                m_ContextSynchronizator = contextSynchronizator;
                m_MessageManager = messageManager;
            }

            public IMessageReceiverStep<TMessageMetadata> WithMessageReceiver(IClientMessageReceiver messageReceiver)
            {
                return new MessageReceiverStep<TMessageMetadata>(m_ContextSynchronizator, m_MessageManager, messageReceiver);
            }
        }

        public interface IMessageReceiverStep<TMessageMetadata>
            : IBuildStep<ClientEngine<TMessageMetadata>>, IOptionalLoggerStep<IMessageReceiverStep<TMessageMetadata>>
            where TMessageMetadata : unmanaged, IMessageMetadata
        {
        }

        private class MessageReceiverStep<TMessageMetadata>
            : IMessageReceiverStep<TMessageMetadata>
            where TMessageMetadata : unmanaged, IMessageMetadata
        {
            public IDirectiveNetcodeLogger Logger { get; set; }

            private readonly IClientContextSynchronizator m_ContextSynchronizator;
            private readonly IClientMessageReceiver m_MessageReceiver;
            private readonly IClientMessageManager<TMessageMetadata> m_MessageManager;

            public MessageReceiverStep(
                IClientContextSynchronizator contextSynchronizator,
                IClientMessageManager<TMessageMetadata> messageManager,
                IClientMessageReceiver messageReceiver)
            {
                m_ContextSynchronizator = contextSynchronizator;
                m_MessageManager = messageManager;
                m_MessageReceiver = messageReceiver;

                Logger = new DirectiveNetcodeLogger(Debug.unityLogger);
            }

            public ClientEngine<TMessageMetadata> Build()
            {
                return new ClientEngine<TMessageMetadata>(
                    Logger ?? new DirectiveNetcodeLogger(Debug.unityLogger),
                    m_ContextSynchronizator,
                    m_MessageReceiver,
                    m_MessageManager);
            }

            public IMessageReceiverStep<TMessageMetadata> UseLogger(IDirectiveNetcodeLogger logger)
            {
                Logger = logger;
                return this;
            }
        }
    }
}
