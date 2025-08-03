using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging.Pipelines
{
    /// <summary>
    /// Represents a message pipeline specifically designed for processing incoming messages from clients to the server.
    /// This pipeline executes a sequence of <see cref="IClientToServerReceiveStep"/> steps to validate and process client messages.
    /// </summary>
    public class ClientToServerReceivePipeline : MessagePipeline<IClientToServerReceiveStep, MessageReceiveParams>
    {
        /// <summary>
        /// Handles an incoming message from a client by executing the pipeline steps with the provided parameters.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection that sent the message.</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
        /// <param name="stream">The data stream reader containing the message data.</param>
        /// <returns>A <see cref="PipelineResult"/> indicating the result of processing the message through the pipeline.</returns>
        public PipelineResult HandleIncomingMessage(ulong connectionUID, MessageMetadataHandler messageMetadata, ref DataStreamReader stream)
        {
            MessageReceiveParams messageParams = new(connectionUID, messageMetadata, ref stream);

            return ExecuteSteps(messageParams);
        }
    }
}
