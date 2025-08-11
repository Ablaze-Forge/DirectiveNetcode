using Unity.Collections;
using UnityEngine;
using AblazeForge.DirectiveNetcode.Unity.Extensions;
using AblazeForge.DirectiveNetcode.Messaging.Pipelines;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Concrete implementation of <see cref="ClientMessageReceiverBase"/> that handles incoming messages from clients.
    /// This class processes incoming data through a message pipeline and dispatches messages to registered handlers.
    /// </summary>
    public class ClientMessageReceiver : ClientMessageReceiverBase
    {
        /// <summary>
        /// The logger instance used for logging messages and errors from the receiver.
        /// </summary>
        private readonly ILogger m_Logger;

        /// <summary>
        /// The message dispatcher used to route messages to their appropriate handlers.
        /// </summary>
        private readonly MessageDispatcher m_MessageDispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerMessageReceiver"/> class with the specified logger, message pipeline, and message dispatcher.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging messages and errors.</param>
        /// <param name="messagePipeline">The server-to-client receive pipeline to use for processing incoming messages.</param>
        /// <param name="messageDispatcher">The message dispatcher to use for routing messages to handlers.</param>
        public ClientMessageReceiver(ILogger logger, ServerToClientReceivePipeline messagePipeline, MessageDispatcher messageDispatcher) : base(messagePipeline)
        {
            m_Logger = logger;
            m_MessageDispatcher = messageDispatcher;
        }

        /// <summary>
        /// Handles an incoming data message from the server by processing it through the message pipeline and dispatching it to registered handlers.
        /// </summary>
        /// <param name="stream">The data stream reader containing the message data.</param>
        /// <returns>A <see cref="MessageResult"/> indicating how the message should be handled.</returns>
        public override MessageResult HandleDataMessage(ref DataStreamReader stream)
        {
            if (!stream.CanRead(sizeof(byte)))
            {
                return MessageResult.KeepAlive;
            }

            MessageMetadataHandler messageMetadata = new(stream.ReadByte());

            PipelineResult pipelineResult = MessagePipeline.HandleIncomingMessage(0, messageMetadata, ref stream);

            if (pipelineResult == PipelineResult.DisconnectClient)
            {
                return MessageResult.Disconnect;
            }

            if (pipelineResult == PipelineResult.DiscardMessage)
            {
                return MessageResult.KeepAlive;
            }

            if (!stream.CanRead(sizeof(ushort)))
            {
                return MessageResult.KeepAlive;
            }

            ushort messageKey = stream.ReadUShort();

            if (messageMetadata.IsDefaultType)
            {
                m_MessageDispatcher.DispatchMessage(messageKey, 0, messageMetadata, ref stream);
            }

            return MessageResult.Success;
        }
    }
}
