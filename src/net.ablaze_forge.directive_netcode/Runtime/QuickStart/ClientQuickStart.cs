using AblazeForge.DirectiveNetcode.Engines;
using AblazeForge.DirectiveNetcode.Logging;
using AblazeForge.DirectiveNetcode.Messaging;
using AblazeForge.DirectiveNetcode.Messaging.Pipelines;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.QuickStart
{
    /// <summary>
    /// Provides a fluent configuration interface for quickly setting up a client network engine with all necessary components including message dispatching, receiving, and sending capabilities.
    /// </summary>
    /// <remarks>
    /// This class offers a step-by-step builder pattern for configuring client-side networking components.
    /// Each configuration step returns a specialized configuration object that guides the setup process.
    /// </remarks>
    public static class ClientQuickstart
    {
        /// <summary>
        /// Initializes the client quickstart configuration process with the specified logger.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging messages. If null, Unity's debug logger will be used.</param>
        /// <returns>A <see cref="MessageDispatcherConfigStep"/> instance to continue the configuration process.</returns>
        public static MessageDispatcherConfigStep Initialize(ILogger logger)
        {
            logger ??= Debug.unityLogger;

            return new(logger);
        }

        /// <summary>
        /// Represents the first configuration step for setting up the message dispatcher.
        /// </summary>
        public class MessageDispatcherConfigStep
        {
            readonly ILogger m_Logger;

            internal MessageDispatcherConfigStep(ILogger logger)
            {
                m_Logger = logger;
            }

            /// <summary>
            /// Creates and configures the message dispatcher instance.
            /// </summary>
            /// <param name="dispatcher">The created message dispatcher instance.</param>
            /// <returns>A <see cref="ClientMessageReceiverConfigStep"/> instance to continue the configuration process.</returns>
            public ClientMessageReceiverConfigStep CreateMessageDispatcher(out MessageDispatcher dispatcher)
            {
                dispatcher = new(m_Logger);

                return new(dispatcher, m_Logger);
            }
        }

        /// <summary>
        /// Represents the configuration step for setting up the client message receiver.
        /// </summary>
        public class ClientMessageReceiverConfigStep
        {
            readonly ILogger m_Logger;
            readonly ServerToClientReceivePipeline m_Pipeline = new();
            readonly MessageDispatcher m_MessageDispatcher;

            internal ClientMessageReceiverConfigStep(MessageDispatcher dispatcher, ILogger logger)
            {
                m_MessageDispatcher = dispatcher;
                m_Logger = logger;
            }

            /// <summary>
            /// Adds a processing step to the server-to-client receive pipeline.
            /// </summary>
            /// <param name="step">The processing step to add to the pipeline.</param>
            /// <returns>The current configuration instance for method chaining.</returns>
            public ClientMessageReceiverConfigStep AddPipelineStep(IServerToClientReceiveStep step)
            {
                m_Pipeline.AddStep(step);

                return this;
            }

            /// <summary>
            /// Finalizes the client message receiver configuration and proceeds to the next step.
            /// </summary>
            /// <returns>A <see cref="ClientMessageSenderConfigStep"/> instance to continue the configuration process.</returns>
            public ClientMessageSenderConfigStep FinalizeClientMessageReceiverConfiguration()
            {
                return new(new ClientMessageReceiver(m_Logger, m_Pipeline, m_MessageDispatcher), m_Logger);
            }
        }

        /// <summary>
        /// Represents the configuration step for setting up the client message sender.
        /// </summary>
        public class ClientMessageSenderConfigStep
        {
            readonly ClientMessageReceiverBase m_MessageReceiverBase;
            readonly ILogger m_Logger;
            readonly ClientToServerSendPipeline m_Pipeline = new();

            internal ClientMessageSenderConfigStep(ClientMessageReceiverBase messageReceiverBase, ILogger logger)
            {
                m_MessageReceiverBase = messageReceiverBase;
                m_Logger = logger;
            }

            /// <summary>
            /// Adds a processing step to the client-to-server send pipeline.
            /// </summary>
            /// <param name="step">The processing step to add to the pipeline.</param>
            /// <returns>The current configuration instance for method chaining.</returns>
            public ClientMessageSenderConfigStep AddPipelineStep(IClientToServerSendStep step)
            {
                m_Pipeline.AddStep(step);

                return this;
            }

            /// <summary>
            /// Finalizes the client message sender configuration and proceeds to the engine build step.
            /// </summary>
            /// <returns>An <see cref="EngineBuildStep"/> instance to complete the configuration process.</returns>
            public EngineBuildStep FinalizeServerMessageSenderConfiguration()
            {
                return new(m_MessageReceiverBase, new ClientMessageSender(m_Logger, m_Pipeline), m_Logger);
            }
        }

        /// <summary>
        /// Represents the final configuration step for building the client engine instance.
        /// </summary>
        public class EngineBuildStep
        {
            readonly ClientMessageReceiverBase m_MessageReceiverBase;
            readonly ClientMessageSenderBase m_MessageSenderBase;
            readonly ILogger m_Logger;

            internal EngineBuildStep(ClientMessageReceiverBase messageReceiverBase, ClientMessageSenderBase messageSenderBase, ILogger logger)
            {
                m_MessageReceiverBase = messageReceiverBase;
                m_MessageSenderBase = messageSenderBase;
                m_Logger = logger;
            }

            /// <summary>
            /// Builds and returns the configured client engine instance.
            /// </summary>
            /// <param name="clientEngineInstance">The created client engine instance.</param>
            public void BuildEngine(out ClientEngine clientEngineInstance)
            {
                clientEngineInstance = new(m_MessageReceiverBase, m_MessageSenderBase, new ErrorCodeLogger(m_Logger));
            }
        }
    }
}
