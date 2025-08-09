using AblazeForge.DirectiveNetcode.Messaging.Pipelines;
using AblazeForge.DirectiveNetcode.Unity.Extensions;
using Unity.Collections;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Concrete implementation of <see cref="ServerMessageSenderBase"/> that prepares outgoing messages to clients.
    /// This class processes outgoing data through a message pipeline before sending it to client connections.
    /// </summary>
    public class ServerMessageSender : ServerMessageSenderBase
    {
        /// <summary>
        /// The logger instance used for logging messages and errors from the sender.
        /// </summary>
        private readonly ILogger m_Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerMessageSender"/> class with the specified logger and message pipeline.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging messages and errors.</param>
        /// <param name="messagePipeline">The server-to-client send pipeline to use for processing outgoing messages.</param>
        public ServerMessageSender(ILogger logger, ServerToClientSendPipeline messagePipeline) : base(messagePipeline)
        {
            m_Logger = logger;
        }

        /// <summary>
        /// Prepares a message to be sent to a client connection by processing it through the message pipeline.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection to send the message to.</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
        /// <param name="writer">The data stream writer for the message.</param>
        /// <returns>A <see cref="MessageResult"/> indicating how the message preparation should be handled.</returns>
        public override MessageResult PrepareMessage(ulong connectionUID, MessageMetadataHandler messageMetadata, ref DataStreamWriter writer)
        {
            if (!writer.CanWriteFixedLength(sizeof(byte)))
            {
                return MessageResult.KeepAlive;
            }

            writer.WriteByte(messageMetadata.Data);

            PipelineResult pipelineResult = MessagePipeline.PrepareMessageToSend(connectionUID, messageMetadata, ref writer);

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
    }
}
