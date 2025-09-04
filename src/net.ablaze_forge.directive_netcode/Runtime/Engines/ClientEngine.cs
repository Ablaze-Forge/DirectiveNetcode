using System;
using System.Collections.Generic;
using AblazeForge.DirectiveNetcode.ConnectionData;
using AblazeForge.DirectiveNetcode.Logging;
using AblazeForge.DirectiveNetcode.Messaging;
using AblazeForge.DirectiveNetcode.Unity.Extensions;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Engines
{
    /// <summary>
    /// Represents the core client engine responsible for managing network connections to a server and processing incoming and outgoing messages.
    /// This engine integrates with Unity's PlayerLoop for <c>tick</c>-based updates without requiring a <see cref="MonoBehaviour"/>.
    /// </summary>
    public class ClientEngine : EngineBase
    {
        public EventHandler OnConnect;
        public EventHandler OnDisconnect;

        private NetworkDriver m_Driver;
        private NetworkConnection m_Connection;

        private readonly ClientMessageReceiverBase m_MessageReceiver;
        private readonly ClientMessageSenderBase m_MessageSender;

        private ulong m_LastDataStreamHandlerID = 0;
        private readonly List<ClientDataStreamHandler> m_DataStreamHandlers = new();

        private NetworkPipeline[] m_NetworkPipelines;

        private readonly IConnectionInformationProvider m_ConnectionInformationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientEngine"/> class with the specified logger and update type.
        /// </summary>
        /// <param name="logger">The error code logger instance for logging messages from the engine.</param>
        /// <param name="updateType">
        /// The type of PlayerLoopSystem to inject into (e.g., typeof(FixedUpdate), typeof(Update)).
        /// Defaults to typeof(FixedUpdate) if null.
        /// </param>
        public ClientEngine(ClientMessageReceiverBase messageReceiver, ClientMessageSenderBase messageSender, IConnectionInformationProvider connectionInformationProvider, ErrorCodeLogger logger, Type updateType = null) : base(logger, updateType)
        {
            m_MessageReceiver = messageReceiver;
            m_MessageSender = messageSender;
            m_ConnectionInformationProvider = connectionInformationProvider;
        }

        /// <summary>
        /// The main update loop for the client engine, executed each tick within the PlayerLoop.
        /// This method is responsible for processing network events and updating the client state.
        /// </summary>
        protected override void Tick()
        {
            CleanUnhandledWriters();

            m_Driver.ScheduleUpdate().Complete();

            PopEventsForConnection();
        }

        /// <summary>
        /// Cleans up any unhandled data stream writers by aborting their send operations and clearing the data stream handlers list.
        /// This method ensures that any data streams that were not properly completed are cleaned up to prevent resource leaks.
        /// </summary>
        private void CleanUnhandledWriters()
        {
            foreach (ClientDataStreamHandler handler in m_DataStreamHandlers)
            {
                if (!handler.Handled)
                {
                    m_Driver.AbortSend(handler.UnderlyingWriter);
                }
            }

            m_DataStreamHandlers.Clear();
        }

        /// <summary>
        /// Processes all pending network events for the active connection.
        /// Handles data messages by delegating to the <see cref="ClientMessageReceiverBase"/>.
        /// </summary>
        private void PopEventsForConnection()
        {
            if (!m_Connection.IsCreated)
            {
                return;
            }

            NetworkEvent.Type cmd;

            bool skipProcessingFurtherEventsForTheConnection = false;

            while ((cmd = m_Connection.PopEvent(m_Driver, out DataStreamReader stream)) != NetworkEvent.Type.Empty
                && !skipProcessingFurtherEventsForTheConnection)
            {
                switch (cmd)
                {
                    case NetworkEvent.Type.Connect:
                        m_ConnectionInformationProvider.RegisterConnection(0);
                        OnConnect.Invoke(this, EventArgs.Empty);
                        break;
                    case NetworkEvent.Type.Data:
                        MessageResult result = m_MessageReceiver.HandleDataMessage(ref stream);

                        if (result == MessageResult.Disconnect)
                        {
                            Disconnect();
                            skipProcessingFurtherEventsForTheConnection = true;
                        }
                        break;
                    case NetworkEvent.Type.Disconnect:
                        Logger.Log(this, $"Client disconnected.");
                        m_Connection = default;
                        m_ConnectionInformationProvider.RemoveConnection(0);
                        OnDisconnect.Invoke(this, null);
                        skipProcessingFurtherEventsForTheConnection = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Initiates a disconnection from the server.
        /// </summary>
        /// <returns><c>true</c> if the disconnection was successfully initiated; otherwise, <c>false</c>.</returns>
        private bool Disconnect()
        {
            NetworkConnection activeConnection = m_Connection;
            m_Connection = default;
            if (!(m_Driver.Disconnect(activeConnection) == 0))
            {
                return false;
            }

            OnDisconnect.Invoke(this, null);
            return true;
        }

        /// <summary>
        /// Initializes and starts the client engine, connecting to a specified server endpoint.
        /// </summary>
        /// <param name="settings">The network settings containing driver configurations.</param>
        /// <param name="port">The port of the server to connect to.</param>
        /// <param name="driverConfiguration">
        /// A <see cref="NetworkDriverConfiguration"/> defining the network protocol (UDP/Websocket), IP version (IPv4/IPv6), and port for the client to use.
        /// </param>
        /// <param name="ipAddress">The ip address the client should connect to. Null, empty or invalid strings will default to loopback ip.
        /// Defaults to <c>null<c>.</param>
        /// <returns>
        /// <c>true</c> if the client engine was successfully initialized and the connection attempt was made.
        /// <c>false</c> if the client could not start, for example if a driver configuration was not provided or if the client is already running.
        /// </returns>
        /// <remarks>
        /// This method will return <c>false</c> if:
        /// <list type="bullet">
        ///     <item><description><paramref name="driverConfiguration"/> is <c>null</c>.</description></item>
        ///     <item><description>The client engine has already been started (a NetworkDriver instance is already created).</description></item>
        /// </list>
        /// If this method returns <c>true</c>, it does not guarantee a successful connection. You must listen for connection events to confirm the connection has been established.
        /// </remarks>
        public bool Start(NetworkSettings settings, NetworkDriverConfiguration driverConfiguration, string ipAddress = null)
        {
            if (driverConfiguration == null)
            {
                return false;
            }

            if (m_Driver.IsCreated)
            {
                return false;
            }

            m_DataStreamHandlers.Clear();

            m_LastDataStreamHandlerID = 0;

            m_Driver = driverConfiguration.GetNetworkDriver(settings);

            NetworkPipeline unreliable = m_Driver.CreatePipeline(driverConfiguration.UnreliablePipelineIds.Stages);
            NetworkPipeline reliable = m_Driver.CreatePipeline(driverConfiguration.ReliablePipelineIds.Stages);
            NetworkPipeline unreliableOrdered = m_Driver.CreatePipeline(driverConfiguration.UnreliableSequencedPipelineIds.Stages);
            NetworkPipeline fragmented = m_Driver.CreatePipeline(driverConfiguration.FragmentationPipelineIds.Stages);

            m_NetworkPipelines = new NetworkPipeline[]
            {
                unreliable,
                reliable,
                unreliableOrdered,
                fragmented
            };

            if (string.IsNullOrEmpty(ipAddress) || !NetworkEndpoint.TryParse(ipAddress, driverConfiguration.Port, out NetworkEndpoint endpoint))
            {
                endpoint = driverConfiguration.UseIPv4 ? NetworkEndpoint.LoopbackIpv4 : NetworkEndpoint.LoopbackIpv6;
                endpoint = endpoint.WithPort(driverConfiguration.Port);
            }

            m_Connection = m_Driver.Connect(endpoint);

            return StartTicking();
        }

        /// <summary>
        /// Begins a network message send operation to the connected server, preparing the data stream writer and registering a handler for the operation.
        /// </summary>
        /// <param name="messageId">The identifier of the message type to be sent.</param>
        /// <param name="pipelineIndex">The index of the <see cref="NetworkPipeline"> to use</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message to be sent.</param>
        /// <param name="handler">The data stream handler for managing the send operation.</param>
        /// <returns><c>true</c> if the send operation was successfully initiated and a handler was created; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method handles the initial steps of sending a message to the server. If the message preparation fails for any reason,
        /// the send operation will be aborted and the method will return <c>false</c>.
        /// </remarks>
        private bool BeginSend(ushort messageId, NetworkPipelineIndex pipelineIndex, MessageMetadataHandler messageMetadata, out ClientDataStreamHandler handler)
        {
            handler = null;

            m_Driver.BeginSend(m_NetworkPipelines[(int)pipelineIndex], m_Connection, out DataStreamWriter writer);

            if (!writer.CanWriteFixedLength(sizeof(ushort)))
            {
                m_Driver.AbortSend(writer);
                return false;
            }

            writer.WriteUShort(messageId);

            MessageResult result = m_MessageSender.PrepareMessage(messageMetadata, ref writer);

            switch (result)
            {
                case MessageResult.Disconnect:
                    m_Driver.AbortSend(writer);
                    Disconnect();
                    return false;
                case MessageResult.Success:
                    return RegisterDataStreamHandler(ref writer, out handler);
                default:
                    m_Driver.AbortSend(writer);
                    return false;
            }
        }

        /// <summary>
        /// Begins a network message send operation to the connected server, preparing the data stream writer and registering a handler for the operation.
        /// </summary>
        /// <param name="messageId">The identifier of the message type to be sent.</param>
        /// <param name="pipelineIndex">The index of the <see cref="NetworkPipeline"> to use</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message to be sent.</param>
        /// <param name="handler">The data stream handler for managing the send operation.</param>
        /// <returns><c>true</c> if the send operation was successfully initiated and a handler was created; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method handles the initial steps of sending a message to the server. If the message preparation fails for any reason,
        /// the send operation will be aborted and the method will return <c>false</c>.
        /// </remarks>
        public bool BeginSend(ushort messageId, NetworkPipelineIndex pipelineIndex, out ClientDataStreamHandler handler)
        {
            return BeginSend(messageId, pipelineIndex, MessageMetadataHandler.Default, out handler);
        }

        /// <summary>
        /// Completes a network message send operation by finalizing the data stream and sending it over the network.
        /// </summary>
        /// <param name="handler">The data stream handler for the send operation to complete.</param>
        /// <returns><c>true</c> if the message was successfully sent; otherwise, <c>false</c>.</returns>
        public bool EndSend(ClientDataStreamHandler handler)
        {
            handler.UnderlyingWriter.WriteInt(handler.UnderlyingWriter.Length);

            handler.Handled = true;
            return m_Driver.EndSend(handler.UnderlyingWriter) == 0;
        }

        /// <summary>
        /// Registers a new data stream handler for the current send operation, assigning it a unique handler ID and adding it to the internal handlers list.
        /// </summary>
        /// <param name="writer">The data stream writer to register.</param>
        /// <param name="handler">The registered data stream handler.</param>
        /// <returns><c>true</c> if the handler was successfully registered; otherwise, <c>false</c>.</returns>
        private bool RegisterDataStreamHandler(ref DataStreamWriter writer, out ClientDataStreamHandler handler)
        {
            ulong id;

            unchecked // ensures the m_LastDataStreamHandlerID can wrapp around if it ever reaches ulong.MaxValue even if the application is running in a checked environment.
            {
                id = ++m_LastDataStreamHandlerID;
            }

            handler = new(id, ref writer);

            m_DataStreamHandlers.Add(handler);

            return true;
        }

        /// <summary>
        /// Stops the client engine, removes it from the PlayerLoop, and disposes of all allocated native resources (the network driver and connection).
        /// </summary>
        /// <returns><c>true</c> if the engine was successfully stopped from ticking; otherwise, <c>false</c>.</returns>
        public bool Stop()
        {
            bool successfullyStoppedEngine = false;

            if (State == EngineState.Running)
            {
                successfullyStoppedEngine = StopTicking();
            }

            if (m_Driver.IsCreated)
            {
                m_Driver.Dispose();
                m_Driver = default;
            }

            if (m_Connection.IsCreated)
            {
                m_Connection = default;
            }

            return successfullyStoppedEngine;
        }
    }
}
