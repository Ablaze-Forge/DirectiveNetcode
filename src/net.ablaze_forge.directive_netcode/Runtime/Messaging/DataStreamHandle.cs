using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Represents a handler for managing data stream operations during network message sending on the server. This class tracks the state of a data stream writer and its associated connection.
    /// </summary>
    public class ServerDataStreamHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerDataStreamHandler"/> class with the specified parameters.
        /// </summary>
        /// <param name="id">The unique identifier for this handler instance.</param>
        /// <param name="connectionUID">The unique identifier of the connection this handler is associated with.</param>
        /// <param name="writer">The data stream writer to be managed by this handler.</param>
        public ServerDataStreamHandler(ulong id, ulong connectionUID, ref DataStreamWriter writer)
        {
            ConnectionUID = connectionUID;
            UnderlyingWriter = writer;
        }

        /// <summary>
        /// The underlying data stream writer used for writing network message data.
        /// </summary>
        public DataStreamWriter UnderlyingWriter;

        /// <summary>
        /// The unique identifier for this handler instance.
        /// </summary>
        public readonly ulong HandlerID;

        /// <summary>
        /// Gets or sets a value indicating whether this data stream has been handled (completed or aborted).
        /// </summary>
        public bool Handled = false;

        /// <summary>
        /// The unique identifier of the connection this handler is associated with.
        /// </summary>
        public readonly ulong ConnectionUID;
    }

    /// <summary>
    /// Represents a handler for managing data stream operations for multiple network connections during message sending on the server.
    /// This class associates a data stream writer with a collection of connections for multicasting.
    /// </summary>
    public class ServerMultiDataStreamHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerMultiDataStreamHandler"/> class with the specified parameters.
        /// </summary>
        /// <param name="messageId">The id of the message being sent.</param>
        /// <param name="messageMetadata"></param>
        /// <param name="connectionUIDs">A collection of unique identifiers for the connections this handler is associated with.</param>
        /// <param name="writer">The data stream writer to be managed by this handler.</param>
        public ServerMultiDataStreamHandler(ushort messageId, int networkPipelineIndex, MessageMetadataHandler messageMetadata, IEnumerable<ulong> connectionUIDs, ref DataStreamWriter writer)
        {
            ConnectionUIDs = connectionUIDs.ToArray();
            UnderlyingWriter = writer;
            MessageId = messageId;
            MessageMetadata = messageMetadata;
            NetworkPipelineIndex = networkPipelineIndex;
        }

        /// <summary>
        /// The underlying data stream writer used for writing network message data.
        /// </summary>
        public DataStreamWriter UnderlyingWriter;

        /// <summary>
        /// The array of unique identifiers for the connections this handler is associated with.
        /// </summary>
        public readonly ulong[] ConnectionUIDs;

        /// <summary>
        /// The message Id to be used for the data stream.
        /// </summary>
        public readonly ushort MessageId;

        /// <summary>
        /// The <see cref="MessageMetadataHandler"/> associated with the message to process.
        /// </summary>
        public readonly MessageMetadataHandler MessageMetadata;

        /// <summary>
        /// The index of the <see cref="NetworkPipeline"/> to be used for the message.
        /// </summary>
        public readonly int NetworkPipelineIndex;
    }

    /// <summary>
    /// Represents a handler for a broadcast data stream operation.
    /// This handler is used when a single message needs to be sent to all connections.
    /// </summary>
    public class ServerBroadcastDataStreamHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerBroadcastDataStreamHandler"/> class.
        /// </summary>
        /// <param name="messageId">The id of the message being sent.</param>
        /// <param name="messageMetadata"></param>
        /// <param name="writer">The data stream writer to be managed by this handler.</param>
        public ServerBroadcastDataStreamHandler(ushort messageId, int networkPipelineIndex, MessageMetadataHandler messageMetadata, ref DataStreamWriter writer)
        {
            UnderlyingWriter = writer;
            MessageId = messageId;
            MessageMetadata = messageMetadata;
            NetworkPipelineIndex = networkPipelineIndex;
        }

        /// <summary>
        /// The underlying data stream writer used for writing network message data.
        /// </summary>
        public DataStreamWriter UnderlyingWriter;

        /// <summary>
        /// The message Id to be used for the data stream.
        /// </summary>
        public readonly ushort MessageId;

        /// <summary>
        /// The <see cref="MessageMetadataHandler"/> associated with the message to process.
        /// </summary>
        public readonly MessageMetadataHandler MessageMetadata;

        /// <summary>
        /// The index of the <see cref="NetworkPipeline"/> to be used for the message.
        /// </summary>
        public readonly int NetworkPipelineIndex;
    }

    /// <summary>
    /// Represents a handler for managing data stream operations during network message sending on the client. This class tracks the state of a data stream writer.
    /// </summary>
    public class ClientDataStreamHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientDataStreamHandler"/> class with the specified parameters.
        /// </summary>
        /// <param name="id">The unique identifier for this handler instance.</param>
        /// <param name="writer">The data stream writer to be managed by this handler.</param>
        public ClientDataStreamHandler(ulong id, ref DataStreamWriter writer)
        {
            UnderlyingWriter = writer;
        }

        /// <summary>
        /// The underlying data stream writer used for writing network message data.
        /// </summary>
        public DataStreamWriter UnderlyingWriter;

        /// <summary>
        /// The unique identifier for this handler instance.
        /// </summary>
        public readonly ulong HandlerID;

        /// <summary>
        /// Gets or sets a value indicating whether this data stream has been handled (completed or aborted).
        /// </summary>
        public bool Handled = false;
    }
}
