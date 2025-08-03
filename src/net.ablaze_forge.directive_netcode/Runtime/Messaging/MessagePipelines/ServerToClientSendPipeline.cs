using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging.Pipelines
{
    /// <summary>
    /// Represents a message pipeline specifically designed for processing outgoing messages from the server to clients.
    /// This pipeline executes a sequence of <see cref="IServerToClientSendStep"/> steps to prepare and validate messages before sending.
    /// </summary>
    public class ServerToClientSendPipeline : MessagePipeline<IServerToClientSendStep, MessageSendParams>
    {
        /// <summary>
        /// Prepares a message to be sent from the server by executing the pipeline steps with the provided parameters.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection the message is being sent to.</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
        /// <param name="stream">The data stream writer for the message.</param>
        /// <returns>A <see cref="PipelineResult"/> indicating the result of processing the message through the pipeline.</returns>
        public PipelineResult PrepareMessageToSend(ulong connectionUID, MessageMetadataHandler messageMetadata, ref DataStreamWriter stream)
        {
            MessageSendParams messageParams = new(connectionUID, messageMetadata, ref stream);

            return ExecuteSteps(messageParams);
        }
    }
}
