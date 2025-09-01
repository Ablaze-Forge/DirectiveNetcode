using AblazeForge.DirectiveNetcode.ConnectionData;
using AblazeForge.DirectiveNetcode.Engines;
using AblazeForge.DirectiveNetcode.Logging;
using AblazeForge.DirectiveNetcode.Messaging;
using AblazeForge.DirectiveNetcode.Messaging.Pipelines;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.QuickStart
{
    /// <summary>
    /// Provides a fluent configuration interface for quickly setting up a server network engine with all necessary components including message dispatching, receiving, and sending capabilities.
    /// </summary>
    /// <remarks>
    /// This class offers a step-by-step builder pattern for configuring server-side networking components.
    /// Each configuration step returns a specialized configuration object that guides the setup process.
    /// </remarks>
    public static class ServerQuickstart
    {
        /// <summary>
        /// Initializes the server quickstart configuration process with the specified logger.
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
            /// <returns>A <see cref="ServerMessageReceiverConfigStep"/> instance to continue the configuration process.</returns>
            public ServerMessageReceiverConfigStep CreateMessageDispatcher(out MessageDispatcher dispatcher, MessageSide messageSide = MessageSide.Server)
            {
                IConnectionInformationProvider connectionInformationProvider = new ServerDefaultConnectionInformationProvider();

                dispatcher = new(m_Logger, connectionInformationProvider, messageSide);

                return new(dispatcher, m_Logger);
            }
        }

        /// <summary>
        /// Represents the configuration step for setting up the server message receiver.
        /// </summary>
        public class ServerMessageReceiverConfigStep
        {
            readonly ILogger m_Logger;
            readonly ClientToServerReceivePipeline m_Pipeline = new();
            readonly MessageDispatcher m_MessageDispatcher;

            internal ServerMessageReceiverConfigStep(MessageDispatcher dispatcher, ILogger logger)
            {
                m_MessageDispatcher = dispatcher;
                m_Logger = logger;
            }

            /// <summary>
            /// Adds a processing step to the client-to-server receive pipeline.
            /// </summary>
            /// <param name="step">The processing step to add to the pipeline.</param>
            /// <returns>The current configuration instance for method chaining.</returns>
            public ServerMessageReceiverConfigStep AddPipelineStep(IClientToServerReceiveStep step)
            {
                m_Pipeline.AddStep(step);

                return this;
            }

            /// <summary>
            /// Finalizes the server message receiver configuration and proceeds to the next step.
            /// </summary>
            /// <returns>A <see cref="ServerMessageSenderConfigStep"/> instance to continue the configuration process.</returns>
            public ServerMessageSenderConfigStep FinalizeServerMessageReceiverConfiguration()
            {
                return new(new ServerMessageReceiver(m_Logger, m_Pipeline, m_MessageDispatcher), m_Logger);
            }
        }

        /// <summary>
        /// Represents the configuration step for setting up the server message sender.
        /// </summary>
        public class ServerMessageSenderConfigStep
        {
            readonly ServerMessageReceiverBase m_MessageReceiverBase;
            readonly ILogger m_Logger;
            readonly ServerToClientSendPipeline m_Pipeline = new();

            internal ServerMessageSenderConfigStep(ServerMessageReceiverBase messageReceiverBase, ILogger logger)
            {
                m_MessageReceiverBase = messageReceiverBase;
                m_Logger = logger;
            }

            /// <summary>
            /// Adds a processing step to the server-to-client send pipeline.
            /// </summary>
            /// <param name="step">The processing step to add to the pipeline.</param>
            /// <returns>The current configuration instance for method chaining.</returns>
            public ServerMessageSenderConfigStep AddPipelineStep(IServerToClientSendStep step)
            {
                m_Pipeline.AddStep(step);

                return this;
            }

            /// <summary>
            /// Finalizes the server message sender configuration and proceeds to the engine build step.
            /// </summary>
            /// <returns>An <see cref="EngineBuildStep"/> instance to complete the configuration process.</returns>
            public EngineBuildStep FinalizeServerMessageSenderConfiguration()
            {
                return new(m_MessageReceiverBase, new ServerMessageSender(m_Logger, m_Pipeline), m_Logger);
            }
        }

        /// <summary>
        /// Represents the final configuration step for building the server engine instance.
        /// </summary>
        public class EngineBuildStep
        {
            readonly ServerMessageReceiverBase m_MessageReceiverBase;
            readonly ServerMessageSenderBase m_MessageSenderBase;
            readonly ILogger m_Logger;

            internal EngineBuildStep(ServerMessageReceiverBase messageReceiverBase, ServerMessageSenderBase messageSenderBase, ILogger logger)
            {
                m_MessageReceiverBase = messageReceiverBase;
                m_MessageSenderBase = messageSenderBase;
                m_Logger = logger;
            }

            /// <summary>
            /// Builds and returns the configured server engine instance.
            /// </summary>
            /// <param name="serverEngineInstance">The created server engine instance.</param>
            public void BuildEngine(out ServerEngine serverEngineInstance)
            {
                serverEngineInstance = new(m_MessageReceiverBase, m_MessageSenderBase, new ErrorCodeLogger(m_Logger));
            }
        }
    }
}
