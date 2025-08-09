using AblazeForge.DirectiveNetcode.Messaging.Pipelines;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Abstract base class for server message senders that prepare outgoing messages to clients.
    /// This class provides a foundation for implementing specific message preparation logic for different server implementations.
    /// </summary>
    public abstract class ServerMessageSenderBase
    {
        /// <summary>
        /// The message pipeline used for processing outgoing server-to-client messages.
        /// </summary>
        protected readonly ServerToClientSendPipeline MessagePipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerMessageSenderBase"/> class with the specified message pipeline.
        /// </summary>
        /// <param name="messagePipeline">The server-to-client send pipeline to use for processing outgoing messages.</param>
        public ServerMessageSenderBase(ServerToClientSendPipeline messagePipeline)
        {
            MessagePipeline = messagePipeline;
        }

        /// <summary>
        /// Prepares a message to be sent to a client connection.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection to send the message to.</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
        /// <param name="writer">The data stream writer for the message.</param>
        /// <returns>A <see cref="MessageResult"/> indicating how the message preparation should be handled.</returns>
        public abstract MessageResult PrepareMessage(ulong connectionUID, MessageMetadataHandler messageMetadata, ref DataStreamWriter writer);
    }
}
