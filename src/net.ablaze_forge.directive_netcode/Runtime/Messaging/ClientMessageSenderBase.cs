using AblazeForge.DirectiveNetcode.Messaging.Pipelines;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Abstract base class for server message senders that prepare outgoing messages to the server.
    /// This class provides a foundation for implementing specific message preparation logic for different server implementations.
    /// </summary>
    public abstract class ClientMessageSenderBase
    {
        /// <summary>
        /// The message pipeline used for processing outgoing client-to-server messages.
        /// </summary>
        protected readonly ClientToServerSendPipeline MessagePipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientMessageSenderBase"/> class with the specified message pipeline.
        /// </summary>
        /// <param name="messagePipeline">The client-to-server send pipeline to use for processing outgoing messages.</param>
        public ClientMessageSenderBase(ClientToServerSendPipeline messagePipeline)
        {
            MessagePipeline = messagePipeline;
        }

        /// <summary>
        /// Prepares a message to be sent to the server.
        /// </summary>
        /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
        /// <param name="writer">The data stream writer for the message.</param>
        /// <returns>A <see cref="MessageResult"/> indicating how the message preparation should be handled.</returns>
        public abstract MessageResult PrepareMessage(MessageMetadataHandler messageMetadata, ref DataStreamWriter writer);
    }
}
