using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging.Pipelines
{
    /// <summary>
    /// Represents a message pipeline specifically designed for processing incoming messages from the server to clients.
    /// This pipeline executes a sequence of <see cref="IServerToClientReceiveStep"/> steps to validate and process server messages on the client side.
    /// </summary>
    public class ServerToClientReceivePipeline : MessagePipeline<IServerToClientReceiveStep, MessageReceiveParams>
    {
        /// <summary>
        /// Handles an incoming message from the server by executing the pipeline steps with the provided parameters.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the server connection that sent the message.</param>
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
