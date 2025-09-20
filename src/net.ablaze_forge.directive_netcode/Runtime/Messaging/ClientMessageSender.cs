using AblazeForge.DirectiveNetcode.Messaging.Pipelines;
using AblazeForge.DirectiveNetcode.Unity.Extensions;
using Unity.Collections;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Concrete implementation of <see cref="ClientMessageSenderBase"/> that prepares outgoing messages to the server.
    /// This class processes outgoing data through a message pipeline before sending it to client connections.
    /// </summary>
    public class ClientMessageSender : ClientMessageSenderBase
    {
        /// <summary>
        /// The logger instance used for logging messages and errors from the sender.
        /// </summary>
        private readonly ILogger m_Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientMessageSender"/> class with the specified logger and message pipeline.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging messages and errors.</param>
        /// <param name="messagePipeline">The client-to-server send pipeline to use for processing outgoing messages.</param>
        public ClientMessageSender(ILogger logger, ClientToServerSendPipeline messagePipeline) : base(messagePipeline)
        {
            m_Logger = logger;
        }

        /// <summary>
        /// Prepares a message to be sent to the server by processing it through the message pipeline.
        /// </summary>
        /// <param name="messageId">The id of the message to send.</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
        /// <param name="writer">The data stream writer for the message.</param>
        /// <returns>A <see cref="MessageResult"/> indicating how the message preparation should be handled.</returns>
        public override MessageResult PrepareMessage(ushort messageId, MessageMetadataHandler messageMetadata, ref DataStreamWriter writer)
        {
            if (!writer.CanWriteFixedLength(sizeof(byte) + sizeof(ushort)))
            {
                return MessageResult.KeepAlive;
            }

            writer.WriteByte(messageMetadata.Data);

            writer.WriteUShort(messageId);

            switch (messageMetadata.Type)
            {
                // For now only Default messages run through the pipeline
                case MessageType.Default:
                    PipelineResult pipelineResult = MessagePipeline.PrepareMessageToSend(0, messageMetadata, ref writer);

                    if (pipelineResult == PipelineResult.DisconnectClient)
                        return MessageResult.Disconnect;

                    if (pipelineResult == PipelineResult.DiscardMessage)
                        return MessageResult.KeepAlive;

                    break;

                default:
                    break;
            }

            return MessageResult.Success;
        }
    }
}
