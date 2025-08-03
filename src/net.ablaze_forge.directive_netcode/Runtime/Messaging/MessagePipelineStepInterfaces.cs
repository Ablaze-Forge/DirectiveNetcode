using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Defines a generic pipeline step interface for processing message parameters.
    /// Implementations of this interface can modify or validate message data as it passes through a pipeline.
    /// </summary>
    /// <typeparam name="TMessageParams">The type of message parameters this pipeline step processes.</typeparam>
    public interface IPipelineStep<TMessageParams>
    {
        /// <summary>
        /// Executes the pipeline step with the specified message parameters.
        /// </summary>
        /// <param name="messageParams">The message parameters to process.</param>
        /// <returns>A <see cref="PipelineStepResult"/> indicating the result of the step execution.</returns>
        PipelineStepResult Execute(TMessageParams messageParams);
    }

    /// <summary>
    /// Represents a pipeline step in the client-to-server message receive pipeline.
    /// </summary>
    public interface IClientToServerReceiveStep : IPipelineStep<MessageReceiveParams> { }

    /// <summary>
    /// Represents a pipeline step in the server-to-client message receive pipeline.
    /// </summary>
    public interface IServerToClientReceiveStep : IPipelineStep<MessageReceiveParams> { }

    /// <summary>
    /// Contains parameters for processing received messages in a pipeline.
    /// This class encapsulates connection information, message metadata, and the data stream for incoming messages.
    /// </summary>
    public class MessageReceiveParams
    {
        /// <summary>
        /// Gets the unique identifier of the connection that sent this message.
        /// </summary>
        public readonly ulong ConnectionUID;

        /// <summary>
        /// Gets the metadata handler for this message, containing information about message type and characteristics.
        /// </summary>
        public readonly MessageMetadataHandler MessageMetadata;

        /// <summary>
        /// Gets or sets the data stream reader containing the message data.
        /// </summary>
        public DataStreamReader Stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageReceiveParams"/> class with the specified parameters.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the connection that sent this message.</param>
        /// <param name="messageMetadata">The metadata handler for this message.</param>
        /// <param name="stream">The data stream reader containing the message data.</param>
        public MessageReceiveParams(ulong connectionUID, MessageMetadataHandler messageMetadata, ref DataStreamReader stream)
        {
            ConnectionUID = connectionUID;
            MessageMetadata = messageMetadata;
            Stream = stream;
        }
    }

    /// <summary>
    /// Represents a pipeline step in the client-to-server message send pipeline.
    /// </summary>
    public interface IClientToServerSendStep : IPipelineStep<MessageSendParams> { }

    /// <summary>
    /// Represents a pipeline step in the server-to-client message send pipeline.
    /// </summary>
    public interface IServerToClientSendStep : IPipelineStep<MessageSendParams> { }

    /// <summary>
    /// Contains parameters for processing messages to be sent in a pipeline.
    /// This class encapsulates connection information, message metadata, and the data stream for outgoing messages.
    /// </summary>
    public class MessageSendParams
    {
        /// <summary>
        /// Gets the unique identifier of the connection this message is being sent to.
        /// </summary>
        public readonly ulong ConnectionUID;

        /// <summary>
        /// Gets the metadata handler for this message, containing information about message type and characteristics.
        /// </summary>
        public readonly MessageMetadataHandler MessageMetadata;

        /// <summary>
        /// Gets or sets the data stream writer for this message.
        /// </summary>
        public DataStreamWriter Stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageSendParams"/> class with the specified parameters.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the connection this message is being sent to.</param>
        /// <param name="messageMetadata">The metadata handler for this message.</param>
        /// <param name="stream">The data stream writer for this message.</param>
        public MessageSendParams(ulong connectionUID, MessageMetadataHandler messageMetadata, ref DataStreamWriter stream)
        {
            ConnectionUID = connectionUID;
            MessageMetadata = messageMetadata;
            Stream = stream;
        }
    }

    /// <summary>
    /// Represents the result of executing a single step in a message pipeline.
    /// </summary>
    public enum PipelineStepResult
    {
        /// <summary>
        /// Indicates that the pipeline step executed successfully and processing should continue.
        /// </summary>
        Success,

        /// <summary>
        /// Indicates that the pipeline step failed and the message should be discarded.
        /// </summary>
        Failure,

        /// <summary>
        /// Indicates that the pipeline step determined the client should be disconnected.
        /// </summary>
        DisconnectClient,
    }

    /// <summary>
    /// Represents the overall result of executing a complete message pipeline.
    /// </summary>
    public enum PipelineResult
    {
        /// <summary>
        /// Indicates that the pipeline executed successfully and the message should be processed normally.
        /// </summary>
        Success,

        /// <summary>
        /// Indicates that the pipeline determined the message should be discarded.
        /// </summary>
        DiscardMessage,

        /// <summary>
        /// Indicates that the pipeline determined the client should be disconnected.
        /// </summary>
        DisconnectClient,
    }
}
