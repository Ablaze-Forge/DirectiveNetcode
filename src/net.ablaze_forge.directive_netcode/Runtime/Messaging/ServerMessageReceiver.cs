using Unity.Collections;
using UnityEngine;
using AblazeForge.DirectiveNetcode.Unity.Extensions;
using AblazeForge.DirectiveNetcode.Messaging.Pipelines;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Concrete implementation of <see cref="ServerMessageReceiverBase"/> that handles incoming messages from clients.
    /// This class processes incoming data through a message pipeline and dispatches messages to registered handlers.
    /// </summary>
    public class ServerMessageReceiver : ServerMessageReceiverBase
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
        /// <param name="messagePipeline">The client-to-server receive pipeline to use for processing incoming messages.</param>
        /// <param name="messageDispatcher">The message dispatcher to use for routing messages to handlers.</param>
        public ServerMessageReceiver(ILogger logger, ClientToServerReceivePipeline messagePipeline, MessageDispatcher messageDispatcher) : base(messagePipeline)
        {
            m_Logger = logger;
            m_MessageDispatcher = messageDispatcher;
        }

        /// <summary>
        /// Handles an incoming data message from a client connection by processing it through the message pipeline and dispatching it to registered handlers.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection that sent the message.</param>
        /// <param name="stream">The data stream reader containing the message data.</param>
        /// <returns>A <see cref="MessageResult"/> indicating how the message should be handled.</returns>
        public override MessageResult HandleDataMessage(ulong connectionUID, ref DataStreamReader stream)
        {
            if (!stream.CanRead(sizeof(byte) + sizeof(ushort)))
            {
                return MessageResult.KeepAlive;
            }

            MessageMetadataHandler messageMetadata = new(stream.ReadByte());

            ushort messageKey = stream.ReadUShort();

            return messageMetadata.Type switch
            {
                MessageType.Default => HandleDefaultDataMessage(connectionUID, messageKey, messageMetadata, ref stream),
                MessageType.Event => HandleEventMessage(connectionUID, messageKey, messageMetadata, ref stream),
                MessageType.Control => HandleControlMessage(connectionUID, messageKey, messageMetadata, ref stream),
                _ => MessageResult.KeepAlive,
            };
        }

        /// <summary>
        /// Handles a default data message by passing it through the message pipeline and dispatching it to registered handlers.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection.</param>
        /// <param name="messageKey">The key identifying the message type.</param>
        /// <param name="messageMetadata">The metadata handler for the message.</param>
        /// <param name="stream">The data stream reader containing the message data.</param>
        /// <returns>A <see cref="MessageResult"/> indicating the result of handling the message.</returns>
        private MessageResult HandleDefaultDataMessage(ulong connectionUID, ushort messageKey, MessageMetadataHandler messageMetadata, ref DataStreamReader stream)
        {
            MessageResult pipelineResult = PassMessageToPipeline(connectionUID, messageMetadata, ref stream);

            if (pipelineResult != MessageResult.Success)
            {
                return pipelineResult;
            }

            m_MessageDispatcher.DispatchMessage(messageKey, connectionUID, messageMetadata, ref stream);

            return MessageResult.Success;
        }

        /// <summary>
        /// Handles an event message by validating the stream length and invoking the event through the message dispatcher.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection.</param>
        /// <param name="messageKey">The key identifying the event type.</param>
        /// <param name="messageMetadata">The metadata handler for the event.</param>
        /// <param name="stream">The data stream reader containing the event data.</param>
        /// <returns>A <see cref="MessageResult"/> indicating the result of handling the event.</returns>
        private MessageResult HandleEventMessage(ulong connectionUID, ushort messageKey, MessageMetadataHandler messageMetadata, ref DataStreamReader stream)
        {
            if (stream.Length != (sizeof(ushort) + sizeof(byte)))
            {
                return MessageResult.KeepAlive;
            }

            m_MessageDispatcher.InvokeEvent(messageKey, connectionUID, messageMetadata);

            return MessageResult.Success;
        }

        /// <summary>
        /// Handles a control message by dispatching it through the message dispatcher.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection.</param>
        /// <param name="messageKey">The key identifying the control message type.</param>
        /// <param name="messageMetadata">The metadata handler for the control message.</param>
        /// <param name="stream">The data stream reader containing the control message data.</param>
        /// <returns>A <see cref="MessageResult"/> indicating the result of handling the control message.</returns>
        private MessageResult HandleControlMessage(ulong connectionUID, ushort messageKey, MessageMetadataHandler messageMetadata, ref DataStreamReader stream)
        {
            m_MessageDispatcher.DispatchControlMessage(messageKey, connectionUID, messageMetadata, ref stream);

            return MessageResult.Success;
        }

        /// <summary>
        /// Passes the incoming message through the message pipeline for processing and returns the appropriate message result based on the pipeline outcome.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection.</param>
        /// <param name="messageMetadata">The metadata handler for the message.</param>
        /// <param name="stream">The data stream reader containing the message data.</param>
        /// <returns>A <see cref="MessageResult"/> indicating the result of passing the message through the pipeline.</returns>
        private MessageResult PassMessageToPipeline(ulong connectionUID, MessageMetadataHandler messageMetadata, ref DataStreamReader stream)
        {
            PipelineResult pipelineResult = MessagePipeline.HandleIncomingMessage(connectionUID, messageMetadata, ref stream);

            if (pipelineResult == PipelineResult.DisconnectClient)
            {
                return MessageResult.Disconnect;
            }

            if (pipelineResult == PipelineResult.DiscardMessage)
            {
                return MessageResult.KeepAlive;
            }

            return MessageResult.Success;
        }

        /// <summary>
        /// Handles a disconnect message from a client connection.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection that disconnected.</param>
        /// <param name="stream">The data stream reader containing any disconnect message data.</param>
        public override void HandleDisconnectMessage(ulong connectionUID, ref DataStreamReader stream)
        {
            throw new System.NotImplementedException();
        }
    }
}
