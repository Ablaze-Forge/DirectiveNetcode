using AblazeForge.DirectiveNetcode.Messaging;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Engines
{
    public abstract class ServerDataSendHandler
    {
        /// <summary>
        /// Gets or sets a value indicating whether this data stream has been handled (completed or aborted).
        /// </summary>
        public bool Handled = false;

        public abstract void Abort(ref MultiNetworkDriver driver);
    }

    /// <summary>
    /// Represents a handler for managing data stream operations during network message sending on the server. This class tracks the state of a data stream writer and its associated connection.
    /// </summary>
    public class ServerDataStreamHandler : ServerDataSendHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerDataStreamHandler"/> class with the specified parameters.
        /// </summary>
        /// <param name="id">The unique identifier for this handler instance.</param>
        /// <param name="connectionUID">The unique identifier of the connection this handler is associated with.</param>
        /// <param name="writer">The data stream writer to be managed by this handler.</param>
        public ServerDataStreamHandler(ulong id, ulong connectionUID, ref DataStreamWriter writer)
        {
            HandlerID = id;
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
        /// The unique identifier of the connection this handler is associated with.
        /// </summary>
        public readonly ulong ConnectionUID;

        public override void Abort(ref MultiNetworkDriver driver)
        {
            driver.AbortSend(UnderlyingWriter);
            Handled = true;
        }
    }

    public class ServerMultiTargetDataStreamHandler : ServerDataSendHandler
    {
        public DataStreamWriter[] Writers { get; private set; }

        public ServerMultiTargetDataStreamHandler(DataStreamWriter[] writers)
        {
            Writers = writers;
        }

        public int Write<T>(T value)
        {
            int failedWrites = 0;

            for (int i = 0; i < Writers.Length; i++)
            {
                if (!Writers[i].IsCreated)
                {
                    continue;
                }

                if (!Writers[i].Write(value))
                {
                    failedWrites++;
                }
            }

            return failedWrites;
        }

        public override void Abort(ref MultiNetworkDriver driver)
        {
            for (int i = 0; i < Writers.Length; i++)
            {
                DataStreamWriter writer = Writers[i];

                if (!writer.IsCreated)
                {
                    continue;
                }

                driver.AbortSend(writer);
            }

            Handled = true;
        }
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
