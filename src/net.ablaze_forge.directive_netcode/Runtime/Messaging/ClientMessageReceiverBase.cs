using AblazeForge.DirectiveNetcode.Messaging.Pipelines;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Abstract base class for server message receivers that process incoming messages from the server.
    /// This class provides a foundation for implementing specific message handling logic for different server implementations.
    /// </summary>
    public abstract class ClientMessageReceiverBase
    {
        /// <summary>
        /// The message pipeline used for processing incoming server-to-client messages.
        /// </summary>
        protected readonly ServerToClientReceivePipeline MessagePipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientMessageReceiverBase"/> class with the specified message pipeline.
        /// </summary>
        /// <param name="messagePipeline">The server-to-client receive pipeline to use for processing incoming messages.</param>
        public ClientMessageReceiverBase(ServerToClientReceivePipeline messagePipeline)
        {
            MessagePipeline = messagePipeline;
        }

        /// <summary>
        /// Handles an incoming data message from a client connection.
        /// </summary>
        /// <param name="stream">The data stream reader containing the message data.</param>
        /// <returns>A <see cref="MessageResult"/> indicating how the message should be handled.</returns>
        public abstract MessageResult HandleDataMessage(ref DataStreamReader stream);
    }
}
