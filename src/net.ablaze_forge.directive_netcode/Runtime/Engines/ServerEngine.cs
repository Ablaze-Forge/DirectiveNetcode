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
    /// Represents the core server engine responsible for managing network connections, processing incoming and outgoing messages, and handling the server lifecycle.
    /// This engine integrates with Unity's PlayerLoop for <c>tick</c>-based updates without requiring a <see cref="MonoBehaviour"/>.
    /// </summary>
    public class ServerEngine : EngineBase
    {
        /// <summary>
        /// Event triggered when a client successfully connects to the server.
        /// </summary>
        public EventHandler<ClientConnectionEventArgs> OnClientConnected;

        /// <summary>
        /// Event triggered when a client disconnects from the server.
        /// </summary>
        public EventHandler<ClientConnectionEventArgs> OnClientDisconnected;

        /// <summary>
        /// Provides data for client connection events, including the unique client identifier.
        /// </summary>
        public class ClientConnectionEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the unique identifier of the connected client.
            /// </summary>
            public ulong ClientUID { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="ClientConnectionEventArgs"/> class with the specified client UID.
            /// </summary>
            /// <param name="clientUID">The unique identifier of the client.</param>
            public ClientConnectionEventArgs(ulong clientUID)
            {
                ClientUID = clientUID;
            }
        }

        private readonly ServerMessageReceiverBase m_MessageReceiver;
        private readonly ServerMessageSenderBase m_MessageSender;

        private MultiNetworkDriver m_Drivers;
        private NativeList<NetworkConnectionHandler> m_Connections;

        private NativeParallelHashMap<ulong, UIDExpirationTracker> m_UIDToTracker;
        private ulong m_LastIssuedUID;

        private int m_MaxConnections;

        private ulong m_LastDataStreamHandlerID = 0;
        private readonly List<ServerDataStreamHandler> m_DataStreamHandlers = new();

        private readonly IConnectionInformationProvider m_ConnectionInformationProvider;

        private NetworkPipeline[] m_NetworkPipelines;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEngine"/> class.
        /// </summary>
        /// <param name="messageReceiver">The <see cref="ServerMessageReceiverBase"/> implementation to handle incoming data and disconnect messages.</param>
        /// <param name="messageSender">The <see cref="ServerMessageSenderBase"/> implementation for sending messages to connected clients.</param>
        /// <param name="logger">The <see cref="ILogger"/> instance for logging messages from the engine.</param>
        /// <param name="updateType">
        /// The type of PlayerLoopSystem to inject into (e.g., typeof(FixedUpdate), typeof(Update)).
        /// Defaults to typeof(FixedUpdate) if null.
        /// </param>
        public ServerEngine(ServerMessageReceiverBase messageReceiver, ServerMessageSenderBase messageSender, IConnectionInformationProvider connectionInformationProvider, ErrorCodeLogger logger, Type updateType = null) : base(logger, updateType)
        {
            m_MessageReceiver = messageReceiver;
            m_MessageSender = messageSender;
            m_ConnectionInformationProvider = connectionInformationProvider;
        }

        /// <summary>
        /// The main update loop for the server engine, executed each tick within the PlayerLoop.
        /// This method schedules network updates, cleans up disconnected connections, accepts new connections, and processes incoming network events.
        /// </summary>
        protected override void Tick()
        {
            CleanUnhandledWriters();

            m_Drivers.ScheduleUpdate().Complete();

            CleanConnections();
            AcceptNewConnections();
            PopEventsForConnections();
        }

        /// <summary>
        /// Cleans up any unhandled data stream writers by aborting their send operations and clearing the data stream handlers list.
        /// This method ensures that any data streams that were not properly completed are cleaned up to prevent resource leaks.
        /// </summary>
        private void CleanUnhandledWriters()
        {
            foreach (ServerDataStreamHandler handler in m_DataStreamHandlers)
            {
                if (!handler.Handled)
                {
                    m_Drivers.AbortSend(handler.UnderlyingWriter);
                }
            }

            m_DataStreamHandlers.Clear();
        }

        /// <summary>
        /// Iterates through the list of active connections and removes any that are no longer created (i.e., have been disconnected). For removed connections, their UIDs are marked for expiration in the <see cref="m_UIDToTracker"/>.
        /// </summary>
        private void CleanConnections()
        {
            for (int i = m_Connections.Length - 1; i >= 0; i--)
            {
                NetworkConnectionHandler handler = m_Connections[i];

                if (!handler.IsConnectionCreated)
                {
                    ulong connectionUID = handler.ConnectionUID;

                    m_Connections.RemoveAtSwapBack(i);

                    if (m_UIDToTracker.TryGetValue(connectionUID, out UIDExpirationTracker tracker))
                    {
                        // Mark the UID for expiration, allowing it to be cleaned up later by CleanupConnectionTrackers
                        tracker.ExpirationTicks = DateTime.UtcNow.AddMinutes(5).Ticks;

                        OnClientDisconnected.Invoke(this, new(connectionUID));

                        m_ConnectionInformationProvider.RemoveConnection(connectionUID);
                    }
                }
            }
        }

        /// <summary>
        /// Accepts any pending new network connections from the <see cref="MultiNetworkDriver"/>. New connections are assigned a unique UID and added to the active connections list and the UID tracking map, provided the maximum connection limit is not exceeded.
        /// </summary>
        private void AcceptNewConnections()
        {
            NetworkConnection c;

            while ((c = m_Drivers.Accept()) != default)
            {
                if (m_Connections.Length < m_MaxConnections)
                {
                    ulong UID = IssueUID();
                    m_Connections.Add(new(c, UID));
                    m_UIDToTracker.Add(UID, new(UID, c));

                    m_ConnectionInformationProvider.RegisterConnection(UID);

                    OnClientConnected.Invoke(this, new(UID));
                }
                else
                {
                    Logger.LogWarning(this, WarningCodes.ServerEngine_ConnectionDropped_MaxReached, "Dropping currentConnection due to reaching the maximum currentConnection count. Consider if a higher limit would be desired.");

                    // TODO: Send a message back indicating the connections are full.

                    m_Drivers.Disconnect(c);
                }
            }
        }

        /// <summary>
        /// Processes all pending network events for each active connection.
        /// Handles data messages by delegating to the <see cref="ServerMessageReceiverBase"/>, and manages disconnect events by logging and also notifying the <see cref="ServerMessageReceiverBase"/>.
        /// </summary>
        private void PopEventsForConnections()
        {
            for (int id = m_Connections.Length - 1; id >= 0; id--)
            {
                if (!m_Connections[id].IsConnectionCreated)
                {
                    continue;
                }

                NetworkConnectionHandler handler = m_Connections[id];

                NetworkConnection currentConnection = handler.Connection;

                NetworkEvent.Type cmd;

                bool skipProcessingFurtherEventsForThisConnection = false;

                while ((cmd = m_Drivers.PopEventForConnection(currentConnection, out DataStreamReader stream)) != NetworkEvent.Type.Empty
                    && !skipProcessingFurtherEventsForThisConnection)
                {
                    switch (cmd)
                    {
                        case NetworkEvent.Type.Connect:
                            Logger.Log(this, $"Unexpected Connect event for existing currentConnection {currentConnection}.");
                            break;
                        case NetworkEvent.Type.Data:
                            MessageResult result = m_MessageReceiver.HandleDataMessage(handler.ConnectionUID, ref stream);

                            if (result == MessageResult.Disconnect)
                            {
                                Disconnect(handler.ConnectionUID);
                                skipProcessingFurtherEventsForThisConnection = true;
                            }
                            break;
                        case NetworkEvent.Type.Disconnect:
                            Logger.Log(this, $"Client {currentConnection} disconnected.");
                            m_MessageReceiver.HandleDisconnectMessage(handler.ConnectionUID, ref stream);
                            Disconnect(handler.ConnectionUID);
                            skipProcessingFurtherEventsForThisConnection = true;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Initiates a disconnection for a client identified by its unique connection UID.
        /// This method handles the network-level disconnection and updates the internal tracking structures to mark the connection as invalid.
        /// </summary>
        /// <param name="connectionUID">The unique ID of the connection to disconnect.</param>
        /// <returns><c>true</c> if the disconnection was successfully initiated; otherwise, <c>false</c>.</returns>
        public bool Disconnect(ulong connectionUID)
        {
            if (connectionUID < 1)
            {
                Logger.LogWarning(this, WarningCodes.ServerEngine_Disconnect_UIDNotFound, $"Attempted to disconnect NetworkConnection with invalid UID {connectionUID}. UIDs start from 1.");
                return false;
            }

            if (m_UIDToTracker.TryGetValue(connectionUID, out UIDExpirationTracker tracker))
            {
                if (!tracker.Connection.IsCreated)
                {
                    Logger.Log(this, $"Attempted to disconnect client with UID {connectionUID}, but its connection was already invalidated.");
                    return false;
                }

                Logger.Log(this, $"Manually disconnecting client with UID {connectionUID}.");

                NetworkConnection connection = tracker.Connection;

                tracker.Connection = default;
                m_UIDToTracker[connectionUID] = tracker;

                int id = m_Connections.IndexOf(new NetworkConnectionHandler(connection, connectionUID));

                if (id != -1)
                {
                    m_Connections[id] = default;
                }
                else
                {
                    Logger.LogWarning(this, WarningCodes.ServerEngine_ConnectionHandlerMissing, $"Failed to find NetworkConnectionHandler for UID {connectionUID} in active connections list during disconnect.");
                }

                m_Drivers.Disconnect(connection);

                return true;
            }
            else
            {
                Logger.LogWarning(this, WarningCodes.ServerEngine_Disconnect_ConnectionNotRegistered, $"Attempted to disconnect NetworkConnection with unregistered UID {connectionUID}.");
                return false;
            }
        }

        /// <summary>
        /// Periodically cleans up expired <see cref="UIDExpirationTracker"/> entries from the <see cref="m_UIDToTracker"/> map. If an expired tracker still points to an active connection, that connection is also disconnected.
        /// </summary>
        // TODO: Inject on some timer system activate every 2 minutes
        public void CleanupConnectionTrackers()
        {
            foreach (var kvp in m_UIDToTracker)
            {
                UIDExpirationTracker tracker = kvp.Value;

                if (tracker.Expired())
                {
                    if (tracker.Connection.IsCreated)
                    {
                        Disconnect(tracker.ConnectionUID);

                        continue;
                    }

                    m_UIDToTracker.Remove(kvp.Key);
                }
            }
        }

        /// <summary>
        /// Generates and returns a new unique ID for a network connection.
        /// IDs are issued sequentially starting from 1.
        /// </summary>
        /// <returns>A unique <see cref="ulong"/> identifier for a new connection.</returns>
        private ulong IssueUID()
        {
            return ++m_LastIssuedUID; // Pre-increment to ensure UIDs start from 1
        }

        /// <summary>
        /// Initializes and starts the server engine using UDP protocol for network communication.
        /// </summary>
        /// <param name="settings">The network settings containing driver configurations.</param>
        /// <param name="maxPlayers">The maximum number of concurrent connections the server will allow. Must be greater than 0.</param>
        /// <param name="port">The network port to bind to. Defaults to 7777.</param>
        /// <remarks>
        /// This method configures the server to use UDP (User Datagram Protocol) for network communication, which provides low-latency, connectionless communication suitable for real-time applications.
        /// The server will bind to the specified port and begin listening for incoming UDP connections.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxPlayers"/> is less than or equal to 0.</exception>
        public void StartWithUDP(NetworkSettings settings, int maxPlayers, ushort port = 7777)
        {
            NetworkDriverConfiguration<UDPNetworkInterface> driverConfig = new(port);

            Start(settings, maxPlayers, driverConfigurations: driverConfig);
        }

        /// <summary>
        /// Initializes and starts the server engine using WebSocket protocol for network communication.
        /// </summary>
        /// <param name="settings">The network settings containing driver configurations.</param>
        /// <param name="maxPlayers">The maximum number of concurrent connections the server will allow. Must be greater than 0.</param>
        /// <param name="port">The network port to bind to. Defaults to 7778.</param>
        /// <remarks>
        /// This method configures the server to use WebSocket protocol for network communication, which provides full-duplex communication channels over a single TCP connection.
        /// WebSockets are particularly useful for web-based clients and provide reliable, ordered message delivery with built-in framing.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxPlayers"/> is less than or equal to 0.</exception>
        public void StartWithWebSocket(NetworkSettings settings, int maxPlayers, ushort port = 7778)
        {
            NetworkDriverConfiguration<WebSocketNetworkInterface> driverConfig = new(port);

            Start(settings, maxPlayers, driverConfigurations: driverConfig);
        }

        /// <summary>
        /// Initializes and starts the server engine, binding to specified network configurations.
        /// </summary>
        /// <param name="settings">The network settings containing driver configurations.</param>
        /// <param name="maxPlayers">The maximum number of concurrent connections the server will allow. Must be greater than 0.</param>
        /// <param name="stopOnFailure">
        /// If <c>true</c>, the server will immediately stop initialization and dispose all resources if any individual network driver fails to bind to its endpoint.
        /// If <c>false</c> (default), individual driver binding failures will be logged as warnings, and the server will attempt to start with any successfully bound drivers.
        /// </param>
        /// <param name="driverConfigurations">
        /// An array of <see cref="NetworkDriverConfiguration"/> defining the network protocols, ports, and IP versions (IPv4/IPv6) for the server to listen on.
        /// At least 1 and at most 4 configurations must be provided.
        /// </param>
        /// <remarks>
        /// This method will log errors and return if:
        /// <list type="bullet">
        ///     <item><description>The number of <paramref name="driverConfigurations"/> is outside the valid range (1-4).</description></item>
        ///     <item><description>The server engine has already been started (MultiNetworkDriver instance is already created).</description></item>
        ///     <item><description><paramref name="maxPlayers"/> is not greater than 0.</description></item>
        ///     <item><description>No network drivers could be successfully bound to their respective endpoints (even if <paramref name="stopOnFailure"/> is <c>false</c>).</description></item>
        ///     <item><description>A network driver fails to bind and <paramref name="stopOnFailure"/> is <c>true</c>.</description></item>
        /// </list>
        /// If <paramref name="stopOnFailure"/> is <c>false</c>, individual driver binding failures will be logged as warnings, but the server will attempt to start with any successfully bound drivers.
        /// </remarks>
        public void Start(NetworkSettings settings, int maxPlayers, bool stopOnFailure = false, params NetworkDriverConfiguration[] driverConfigurations)
        {
            if (driverConfigurations.Length is > 4 or < 1)
            {
                Logger.LogError(this, ErrorCodes.ServerEngine_InvalidDriverCount, "The server can only be started if at least 1 and at most 4 driver configurations are provided.");
                return;
            }

            if (m_Drivers.IsCreated)
            {
                Logger.LogError(this, ErrorCodes.ServerEngine_MultipleNetworkDrivers, "MultiNetworkDriver instance already created, aborting Start method.");
                return;
            }

            if (maxPlayers == 0)
            {
                Logger.LogError(this, ErrorCodes.ServerEngine_MaxPlayersZero, "The maximum of players must be provided and must be a value greater than 0.");
                return;
            }

            m_DataStreamHandlers.Clear();

            m_LastIssuedUID = 0; // UIDs will start from 1 due to pre-increment in IssueUID()

            m_MaxConnections = maxPlayers;

            m_Drivers = MultiNetworkDriver.Create();

            int nativeContainersInitialSize = (int)MathF.Min(maxPlayers, 16);
            m_Connections = new(nativeContainersInitialSize, Allocator.Persistent);
            m_UIDToTracker = new(nativeContainersInitialSize, Allocator.Persistent);

            for (int i = 0; i < driverConfigurations.Length; i++)
            {
                NetworkDriverConfiguration config = driverConfigurations[i];

                NetworkDriver driver = config.GetNetworkDriver(settings);

                NetworkPipeline unreliable = driver.CreatePipeline(config.UnreliablePipelineIds.Stages);
                NetworkPipeline reliable = driver.CreatePipeline(config.ReliablePipelineIds.Stages);
                NetworkPipeline unreliableOrdered = driver.CreatePipeline(config.UnreliableSequencedPipelineIds.Stages);
                NetworkPipeline fragmented = driver.CreatePipeline(config.FragmentationPipelineIds.Stages);

                if (i == 0)
                {
                    m_NetworkPipelines = new NetworkPipeline[]
                    {
                        unreliable,
                        reliable,
                        unreliableOrdered,
                        fragmented
                    };
                }

                NetworkEndpoint endpoint = config.UseIPv4 ? NetworkEndpoint.AnyIpv4 : NetworkEndpoint.AnyIpv6;

                endpoint = endpoint.WithPort(config.Port);

                if (driver.Bind(endpoint) != 0)
                {
                    Logger.LogWarning(this, WarningCodes.ServerEngine_BindFailed, $"Failed to bind NetworkDriver of {driver.GetType().Name} type at endpoint {endpoint}.");
                    driver.Dispose();

                    if (stopOnFailure)
                    {
                        Logger.LogError(this, ErrorCodes.ServerEngine_DriverBindFailure, $"Due to failure to bind a NetworkDriver, server is stopping and resources are being disposed.");
                        m_Drivers.Dispose();
                        m_Connections.Dispose();
                        m_UIDToTracker.Dispose();
                        break;
                    }

                    continue;
                }

                driver.Listen();

                m_Drivers.AddDriver(driver);
            }

            if (m_Drivers.DriverCount == 0)
            {
                Logger.LogError(this, ErrorCodes.ServerEngine_Start_NoBoundDriver, "No network drivers were successfully bound. The server cannot start without active listeners, please verify all configurations are correct.");
                m_Drivers.Dispose();
                m_Connections.Dispose();
                m_UIDToTracker.Dispose();
                return;
            }

            StartTicking();
        }

        /// <summary>
        /// Begins a network message send operation to the specified client connection, preparing the data stream writer and registering a handler for the operation.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection to send the message to.</param>
        /// <param name="messageId">The identifier of the message type to be sent.</param>
        /// <param name="pipelineIndex">The index of the <see cref="NetworkPipeline"> to use</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message to be sent.</param>
        /// <param name="handler">The data stream handler for managing the send operation.</param>
        /// <returns><c>true</c> if the send operation was successfully initiated; otherwise, <c>false</c>.</returns>
        private bool BeginSend(ulong connectionUID, ushort messageId, NetworkPipelineIndex pipelineIndex, MessageMetadataHandler messageMetadata, out ServerDataStreamHandler handler)
        {
            handler = null;

            if (!m_UIDToTracker.TryGetValue(connectionUID, out UIDExpirationTracker tracker) || tracker.IsDisconnected())
            {
                return false;
            }

            m_Drivers.BeginSend(m_NetworkPipelines[(int)pipelineIndex], tracker.Connection, out DataStreamWriter writer);

            MessageResult result = m_MessageSender.PrepareMessage(connectionUID, messageId, messageMetadata, ref writer);

            switch (result)
            {
                case MessageResult.Disconnect:
                    m_Drivers.AbortSend(writer);
                    Disconnect(connectionUID);
                    return false;
                case MessageResult.Success:
                    return RegisterDataStreamHandler(ref writer, connectionUID, out handler);
                default:
                    m_Drivers.AbortSend(writer);
                    return false;
            }
        }

        /// <summary>
        /// Begins a network message broadcast operation, preparing the data stream writer and registering a handler for the broadcast operation.
        /// This method prepares a message that can be sent to all connected clients simultaneously.
        /// </summary>
        /// <param name="messageId">The identifier of the message type to be broadcast.</param>
        /// <param name="pipelineIndex">The index of the <see cref="NetworkPipeline"> to use</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message to be broadcast.</param>
        /// <param name="handler">The broadcast data stream handler for managing the broadcast operation.</param>
        private void BeginBroadcast(ushort messageId, NetworkPipelineIndex pipelineIndex, MessageMetadataHandler messageMetadata, out ServerBroadcastDataStreamHandler handler)
        {
            DataStreamWriter writer = new();

            handler = new(messageId, (int)pipelineIndex, messageMetadata, ref writer);
        }

        /// <summary>
        /// Begins a network message multi-send operation, preparing the data stream writer and registering a handler for sending to multiple specific clients.
        /// This method prepares a message that can be sent to a specific subset of connected clients.
        /// </summary>
        /// <param name="messageId">The identifier of the message type to be sent.</param>
        /// <param name="pipelineIndex">The index of the <see cref="NetworkPipeline"> to use</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message to be sent.</param>
        /// <param name="connectionUIDs">An enumerable collection of unique identifiers for the client connections to send the message to.</param>
        /// <param name="handler">The multi-send data stream handler for managing the multi-send operation.</param>
        private void BeginMultiSend(ushort messageId, NetworkPipelineIndex pipelineIndex, MessageMetadataHandler messageMetadata, IEnumerable<ulong> connectionUIDs, out ServerMultiDataStreamHandler handler)
        {
            DataStreamWriter writer = new();

            handler = new(messageId, (int)pipelineIndex, messageMetadata, connectionUIDs, ref writer);
        }

        /// <summary>
        /// Begins a network message send operation to the specified client connection, preparing the data stream writer and registering a handler for the operation.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the client connection to send the message to.</param>
        /// <param name="messageId">The identifier of the message type to be sent.</param>
        /// <param name="pipelineIndex">The index of the <see cref="NetworkPipeline"> to use</param>
        /// <param name="handler">The data stream handler for managing the send operation.</param>
        /// <returns><c>true</c> if the send operation was successfully initiated; otherwise, <c>false</c>.</returns>
        public bool BeginSend(ulong connectionUID, ushort messageId, NetworkPipelineIndex pipelineIndex, out ServerDataStreamHandler handler)
        {
            return BeginSend(connectionUID, messageId, pipelineIndex, MessageMetadataHandler.Default, out handler);
        }

        /// <summary>
        /// Begins a network message broadcast operation, preparing the data stream writer and registering a handler for the broadcast operation.
        /// This method prepares a message that can be sent to all connected clients simultaneously.
        /// </summary>
        /// <param name="messageId">The identifier of the message type to be broadcast.</param>
        /// <param name="handler">The broadcast data stream handler for managing the broadcast operation.</param>
        public void BeginBroadcast(ushort messageId, NetworkPipelineIndex pipelineIndex, out ServerBroadcastDataStreamHandler handler)
        {
            BeginBroadcast(messageId, pipelineIndex, MessageMetadataHandler.Default, out handler);
        }

        /// <summary>
        /// Begins a network message multi-send operation, preparing the data stream writer and registering a handler for sending to multiple specific clients.
        /// This method prepares a message that can be sent to a specific subset of connected clients.
        /// </summary>
        /// <param name="messageId">The identifier of the message type to be sent.</param>
        /// <param name="connectionUIDs">An enumerable collection of unique identifiers for the client connections to send the message to.</param>
        /// <param name="handler">The multi-send data stream handler for managing the multi-send operation.</param>
        public void BeginMultiSend(ushort messageId, NetworkPipelineIndex pipelineIndex, IEnumerable<ulong> connectionUIDs, out ServerMultiDataStreamHandler handler)
        {
            BeginMultiSend(messageId, pipelineIndex, MessageMetadataHandler.Default, connectionUIDs, out handler);
        }

        /// <summary>
        /// Completes a network message send operation by finalizing the data stream and sending it over the network.
        /// </summary>
        /// <param name="handler">The data stream handler for the send operation to complete.</param>
        /// <returns><c>true</c> if the message was successfully sent; otherwise, <c>false</c>.</returns>
        public bool EndSend(ServerDataStreamHandler handler)
        {
            handler.UnderlyingWriter.WriteInt(handler.UnderlyingWriter.Length);

            handler.Handled = true;
            return m_Drivers.EndSend(handler.UnderlyingWriter) == 0;
        }

        /// <summary>
        /// Completes a network message multi-send operation by finalizing the data stream and sending it to multiple specific clients.
        /// This method iterates through the specified connection UIDs and sends the prepared message to each one.
        /// </summary>
        /// <param name="handler">The multi-send data stream handler for the send operation to complete.</param>
        /// <returns>The number of messages successfully sent to clients.</returns>
        public int EndSend(ServerMultiDataStreamHandler handler)
        {
            int messagesSent = 0;

            DataStreamWriter writer = handler.UnderlyingWriter;

            for (int i = 0; i < handler.ConnectionUIDs.Length; i++)
            {
                ulong connectionUID = handler.ConnectionUIDs[i];

                if (!m_UIDToTracker.TryGetValue(connectionUID, out UIDExpirationTracker tracker) || tracker.IsDisconnected())
                {
                    continue;
                }

                MessageResult result = m_MessageSender.PrepareMessage(connectionUID, handler.MessageId, handler.MessageMetadata, ref writer);

                switch (result)
                {
                    case MessageResult.Disconnect:
                        m_Drivers.AbortSend(writer);
                        Disconnect(connectionUID);
                        continue;
                    case MessageResult.Success:
                        break;
                    default:
                        m_Drivers.AbortSend(writer);
                        continue;
                }

                if (!writer.CanWrite(handler.UnderlyingWriter.Length))
                {
                    m_Drivers.AbortSend(writer);
                    continue;
                }

                writer.WriteBytes(handler.UnderlyingWriter.AsNativeArray());

                if (m_Drivers.EndSend(writer) == 0)
                {
                    messagesSent++;
                }
            }

            return messagesSent;
        }

        /// <summary>
        /// Completes a network message broadcast operation by finalizing the data stream and sending it to all connected clients.
        /// This method iterates through all active connections and sends the prepared message to each one.
        /// </summary>
        /// <param name="handler">The broadcast data stream handler for the send operation to complete.</param>
        /// <returns>The number of messages successfully sent to clients.</returns>
        public int EndSend(ServerBroadcastDataStreamHandler handler)
        {
            int messagesSent = 0;

            DataStreamWriter writer = handler.UnderlyingWriter;

            for (int i = 0; i < m_Connections.Length; i++)
            {
                NetworkConnectionHandler networkHandler = m_Connections[i];

                if (!networkHandler.IsConnectionCreated)
                {
                    continue;
                }

                MessageResult result = m_MessageSender.PrepareMessage(networkHandler.ConnectionUID, handler.MessageId, handler.MessageMetadata, ref writer);

                switch (result)
                {
                    case MessageResult.Disconnect:
                        m_Drivers.AbortSend(writer);
                        Disconnect(networkHandler.ConnectionUID);
                        continue;
                    case MessageResult.Success:
                        break;
                    default:
                        m_Drivers.AbortSend(writer);
                        continue;
                }

                if (!writer.CanWrite(handler.UnderlyingWriter.Length))
                {
                    m_Drivers.AbortSend(writer);
                    continue;
                }

                writer.WriteBytes(handler.UnderlyingWriter.AsNativeArray());

                if (m_Drivers.EndSend(writer) == 0)
                {
                    messagesSent++;
                }
            }

            return messagesSent;
        }

        /// <summary>
        /// Registers a new data stream handler with the specified writer and connection ID, assigning it a unique handler ID and adding it to the internal handlers list.
        /// </summary>
        /// <param name="writer">The data stream writer to register.</param>
        /// <param name="connectionUID">The unique identifier of the connection this handler is associated with.</param>
        /// <param name="handler">The registered data stream handler.</param>
        /// <returns><c>true</c> if the handler was successfully registered; otherwise, <c>false</c>.</returns>
        private bool RegisterDataStreamHandler(ref DataStreamWriter writer, ulong connectionUID, out ServerDataStreamHandler handler)
        {
            ulong id;

            unchecked // ensures the m_LastDataStreamHandlerID can wrapp around if it ever reaches ulong.MaxValue even if the application is running in a checked environment.
            {
                id = ++m_LastDataStreamHandlerID;
            }

            handler = new(id, connectionUID, ref writer);

            m_DataStreamHandlers.Add(handler);

            return true;
        }

        /// <summary>
        /// Stops the server engine, removes it from the PlayerLoop, and disposes all allocated native resources (network drivers, connection lists, and UID trackers).
        /// </summary>
        /// <returns><c>true</c> if the engine was successfully stopped from ticking; otherwise, <c>false</c>.</returns>
        public bool Stop()
        {
            bool successfullyStoppedEngine = false;

            if (m_UIDToTracker.IsCreated)
            {
                m_UIDToTracker.Dispose();
                m_UIDToTracker = default;
            }

            if (State == EngineState.Running)
            {
                successfullyStoppedEngine = StopTicking();
            }

            if (m_Drivers.IsCreated)
            {
                m_Drivers.Dispose();
                m_Drivers = default;
            }

            if (m_Connections.IsCreated)
            {
                m_Connections.Dispose();
                m_Connections = default;
            }

            return successfullyStoppedEngine;
        }

        /// <summary>
        /// Represents a handler for a network connection, associating a <see cref="NetworkConnection"/> with a unique application-level UID. Implements <see cref="IEquatable{T}"/> for efficient comparison and lookup.
        /// </summary>
        private struct NetworkConnectionHandler : IEquatable<NetworkConnection>, IEquatable<NetworkConnectionHandler>
        {
            /// <summary>
            /// The underlying Unity Network Transport connection.
            /// </summary>
            public NetworkConnection Connection { get; set; }

            /// <summary>
            /// The unique application-level ID assigned to this connection.
            /// </summary>
            public ulong ConnectionUID { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the underlying network connection is currently created and valid.
            /// </summary>
            public readonly bool IsConnectionCreated => Connection.IsCreated;

            /// <summary>
            /// Initializes a new instance of the <see cref="NetworkConnectionHandler"/> struct.
            /// </summary>
            /// <param name="connection">The network connection to wrap.</param>
            /// <param name="connectionUID">The unique ID for this connection.</param>
            public NetworkConnectionHandler(NetworkConnection connection, ulong connectionUID)
            {
                Connection = connection;
                ConnectionUID = connectionUID;
            }

            /// <summary>
            /// Indicates whether the current <see cref="NetworkConnectionHandler"/> is equal to another <see cref="NetworkConnection"/>.
            /// Equality is based solely on the underlying <see cref="Connection"/>.
            /// </summary>
            /// <param name="other">A <see cref="NetworkConnection"/> to compare with this instance.</param>
            /// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
            public readonly bool Equals(NetworkConnection other) => Connection.Equals(other);

            /// <summary>
            /// Indicates whether the current <see cref="NetworkConnectionHandler"/> is equal to another <see cref="NetworkConnectionHandler"/>.
            /// Equality is based on both the underlying <see cref="Connection"/> and the <see cref="ConnectionUID"/>.
            /// </summary>
            /// <param name="other">A <see cref="NetworkConnectionHandler"/> to compare with this instance.</param>
            /// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
            public readonly bool Equals(NetworkConnectionHandler other) => Connection.Equals(other) && ConnectionUID == other.ConnectionUID;

            /// <summary>
            /// Returns the hash code for this instance. The hash code is based on the underlying <see cref="Connection"/>'s hash code.
            /// </summary>
            /// <returns>A 32-bit signed integer hash code.</returns>
            public override readonly int GetHashCode() => Connection.GetHashCode();
        }

        /// <summary>
        /// Tracks the expiration status of a unique connection ID, primarily used for managing the lifecycle of UIDs after a connection has disconnected.
        /// </summary>
        private struct UIDExpirationTracker
        {
            /// <summary>
            /// The unique application-level ID for the connection this tracker pertains to.
            /// </summary>
            public ulong ConnectionUID { get; private set; }

            /// <summary>
            /// The associated network connection. This may be an invalid/default connection if the client has disconnected.
            /// </summary>
            public NetworkConnection Connection { get; set; }

            /// <summary>
            /// The UTC ticks at which this UID is considered expired. A value of 0 means no expiration.
            /// </summary>
            public long ExpirationTicks { get; set; }

            /// <summary>
            /// Checks if the tracker's expiration time has passed.
            /// </summary>
            /// <returns><c>true</c> if the tracker has an expiration time set and it has passed; otherwise, <c>false</c>.</returns>
            public readonly bool Expired()
            {
                return ExpirationTicks > 0 && DateTime.UtcNow.Ticks > ExpirationTicks;
            }

            /// <summary>
            /// Checks if the associated network connection is disconnected or if the tracker has expired.
            /// </summary>
            /// <returns><c>true</c> if the connection is not created or the tracker has expired; otherwise, <c>false</c>.</returns>
            public readonly bool IsDisconnected()
            {
                return !Connection.IsCreated || (ExpirationTicks > 0 && DateTime.UtcNow.Ticks > ExpirationTicks);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="UIDExpirationTracker"/> struct.
            /// </summary>
            /// <param name="uid">The unique ID of the connection.</param>
            /// <param name="connection">The network connection associated with the UID.</param>
            public UIDExpirationTracker(ulong uid, NetworkConnection connection)
            {
                ConnectionUID = uid;
                Connection = connection;
                ExpirationTicks = 0;
            }
        }
    }
}
