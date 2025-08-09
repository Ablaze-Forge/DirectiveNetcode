using AblazeForge.DirectiveNetcode.Messaging.Pipelines;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Abstract base class for server message receivers that process incoming messages from clients.
    /// This class provides a foundation for implementing specific message handling logic for different server implementations.
    /// </summary>
    public abstract class ServerMessageReceiverBase
    {
        /// <summary>
        /// The message pipeline used for processing incoming client-to-server messages.
        /// </summary>
        protected readonly ClientToServerReceivePipeline MessagePipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerMessageReceiverBase"/> class with the specified message pipeline.
        /// </summary>
        /// <param name="messagePipeline">The client-to-server receive pipeline to use for processing incoming messages.</param>
        public ServerMessageReceiverBase(ClientToServerReceivePipeline messagePipeline)
        {
            MessagePipeline = messagePipeline;
        }

        /// <summary>
        /// Handles an incoming data message from a client connection.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection that sent the message.</param>
        /// <param name="stream">The data stream reader containing the message data.</param>
        /// <returns>A <see cref="MessageResult"/> indicating how the message should be handled.</returns>
        public abstract MessageResult HandleDataMessage(ulong connectionUID, ref DataStreamReader stream);

        /// <summary>
        /// Handles a disconnect message from a client connection.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection that disconnected.</param>
        /// <param name="stream">The data stream reader containing any disconnect message data.</param>
        public abstract void HandleDisconnectMessage(ulong connectionUID, ref DataStreamReader stream);
    }
}
