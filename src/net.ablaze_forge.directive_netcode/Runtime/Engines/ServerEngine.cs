using AblazeForge.DirectiveNetcode.ConnectionData;
using AblazeForge.DirectiveNetcode.Logging;
using AblazeForge.DirectiveNetcode.Messaging;
using AblazeForge.DirectiveNetcode.Utilities;
using System;
using Unity.Collections;
using Unity.Networking.Transport;

namespace AblazeForge.DirectiveNetcode.Engines
{
    public sealed class ServerEngine<TId, TMessageMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue> : TickSystem
        where TId : unmanaged, IEquatable<TId>
        where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        private readonly object m_InitializationStopLock = new();

        private MultiNetworkDriver m_Drivers;
        private readonly IServerContextSynchronizator m_ContextSynchronizator;
        private readonly IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> m_ConnectionManager;
        private readonly IServerMessageManager<TId, TMessageMetadata> m_MessageManager;
        private readonly IServerMessageReceiver<TId> m_MessageReceiver;

        public ServerEngine(IDirectiveNetcodeLogger logger, IServerContextSynchronizator contextSynchronizator, IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> connectionManager, IServerMessageManager<TId, TMessageMetadata> messageManager, IServerMessageReceiver<TId> messageReceiver, Type updateType = null) : base(logger, updateType)
        {
            m_ContextSynchronizator = contextSynchronizator;
            m_ConnectionManager = connectionManager;
            m_MessageReceiver = messageReceiver;
            m_MessageManager = messageManager;
        }

        protected override void Tick()
        {
            m_ContextSynchronizator.MoveToStartState();

            m_Drivers.ScheduleUpdate().Complete();

            ServerContextSynchronizationState currentState;

            while ((currentState = m_ContextSynchronizator.MoveNext()) != ServerContextSynchronizationState.TickEnded)
            {
                switch (currentState)
                {
                    case ServerContextSynchronizationState.CleanningConnections:
                        m_ConnectionManager.CleanInnactiveConnections();
                        break;
                    case ServerContextSynchronizationState.AuthenticationRequests:
                        // Make authentication requests
                        break;
                    case ServerContextSynchronizationState.AddingNewConnections:
                        AcceptNewConnections();
                        break;
                    case ServerContextSynchronizationState.ProcessingMessages:
                        ProcessMessages();
                        break;
                }
            }
        }

        private void AcceptNewConnections()
        {
            NetworkConnection connection;

            while ((connection = m_Drivers.Accept()) != default)
            {
                if (!m_ConnectionManager.AddConnection(connection, out _))
                {
                    m_Drivers.Disconnect(connection);
                }
            }
        }

        private void ProcessMessages()
        {
            NativeArray<TId> connectionIds = m_ConnectionManager.GetAllConnectionIds();

            for (int i = 0; i < connectionIds.Length; i++)
            {
                TId connectionId = connectionIds[i];

                NetworkConnection connection = m_ConnectionManager.GetNetworkConnection(connectionId);

                if (connection == default || !connection.IsCreated)
                {
                    continue;
                }

                NetworkEvent.Type cmd;

                while ((cmd = m_Drivers.PopEventForConnection(connection, out DataStreamReader reader)) != NetworkEvent.Type.Empty)
                {
                    switch (cmd)
                    {
                        case NetworkEvent.Type.Data:
                            m_MessageReceiver.HandleDataMessage(connectionId, ref reader);
                            break;
                        case NetworkEvent.Type.Disconnect:
                            m_ConnectionManager.RemoveConnection(connectionId, out _);
                            break;
                        case NetworkEvent.Type.Connect:
                            Logger.LogWarning(GetType().Name, WarningCodes.Undefined, "Received Connect event while processing messages. This should not happen.");
                            break;
                    }
                }
            }
        }

        public bool Start(
            NetworkDriverConfiguration driverConfiguration,
            int maxConnections = 0,
            NetworkDriverConfiguration driver2Configuration = null,
            NetworkDriverConfiguration driver3Configuration = null,
            NetworkDriverConfiguration driver4Configuration = null)
        {
            NetworkPipeline unreliablePipe = default;
            NetworkPipeline reliablePipe = default;
            NetworkPipeline unreliableSequencedPipe = default;
            NetworkPipeline fragmentedPipe = default;
            bool pipelinesConfigured = false;

            lock (m_InitializationStopLock)
            {
                if (maxConnections < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxConnections), "Max connections must be greater than or equal to zero.");
                }

                if (m_Drivers.IsCreated)
                {
                    Logger.LogError(GetType().Name, ErrorCodes.ServerEngine_MultipleNetworkDrivers, "A network driver is already created for this server engine. Call Stop() before attempting to restart a engine.");
                }

                if (maxConnections == 0)
                {
                    maxConnections = int.MaxValue;
                }

                if (driverConfiguration == null)
                {
                    throw new ArgumentNullException(nameof(driverConfiguration), "The first driver configuration is required.");
                }

                m_Drivers = MultiNetworkDriver.Create();

                bool createdDriver1 = TryCreateDriverAndListen(driverConfiguration, out NetworkDriver driver1);
                bool createdDriver2 = TryCreateDriverAndListen(driver2Configuration, out NetworkDriver driver2);
                bool createdDriver3 = TryCreateDriverAndListen(driver3Configuration, out NetworkDriver driver3);
                bool createdDriver4 = TryCreateDriverAndListen(driver4Configuration, out NetworkDriver driver4);

                if (!(createdDriver1 || createdDriver2 || createdDriver3 || createdDriver4))
                {
                    Logger.LogError(GetType().Name, ErrorCodes.Undefined, "No network driver started.");

                    m_Drivers.Dispose();

                    return false;
                }

                if (driver1.IsCreated) m_Drivers.AddDriver(driver1);
                if (driver2.IsCreated) m_Drivers.AddDriver(driver2);
                if (driver3.IsCreated) m_Drivers.AddDriver(driver3);
                if (driver4.IsCreated) m_Drivers.AddDriver(driver4);

                if (m_Drivers.DriverCount == 0)
                {
                    Logger.LogError(GetType().Name, ErrorCodes.ServerEngine_Start_NoBoundDriver, "No network drivers were successfully bound. The server cannot start without active listeners, please verify all configurations are correct.");

                    goto cancel;
                }

                m_ConnectionManager.Initialize(maxConnections);
                m_MessageManager.Initialize(m_Drivers, unreliablePipe, reliablePipe, unreliableSequencedPipe, fragmentedPipe);

                if (!StartTicking())
                {
                    goto cancel;
                }

                return true;

            cancel:
                Stop();

                driver1.Dispose();
                driver2.Dispose();
                driver3.Dispose();
                driver4.Dispose();

                return false;
            }

            bool TryCreateDriverAndListen(NetworkDriverConfiguration configuration, out NetworkDriver driver)
            {
                if (configuration == null)
                {
                    driver = default;
                    return false;
                }

                driver = configuration.GetNetworkDriver();

                if (driver.IsCreated)
                {
                    if (!pipelinesConfigured)
                    {
                        unreliablePipe = driver.CreatePipeline(configuration.UnreliablePipelineIds.Stages);
                        reliablePipe = driver.CreatePipeline(configuration.ReliablePipelineIds.Stages);
                        unreliableSequencedPipe = driver.CreatePipeline(configuration.UnreliableSequencedPipelineIds.Stages);
                        fragmentedPipe = driver.CreatePipeline(configuration.FragmentationPipelineIds.Stages);
                    }
                    else
                    {
                        _ = driver.CreatePipeline(configuration.UnreliablePipelineIds.Stages);
                        _ = driver.CreatePipeline(configuration.ReliablePipelineIds.Stages);
                        _ = driver.CreatePipeline(configuration.UnreliableSequencedPipelineIds.Stages);
                        _ = driver.CreatePipeline(configuration.FragmentationPipelineIds.Stages);
                    }

                    NetworkEndpoint endpoint = (configuration.IsIPv4 ? NetworkEndpoint.AnyIpv4 : NetworkEndpoint.AnyIpv6).WithPort(configuration.Port);

                    driver.Bind(endpoint);

                    int listenResult = driver.Listen();

                    if (listenResult == 0)
                    {
                        return true;
                    }
                    else
                    {
                        Logger.LogError(GetType().Name, ErrorCodes.Undefined, $"Driver failed to bind/listen. Error Code: {listenResult}");
                        driver.Dispose();
                        return false;
                    }
                }

                return false;
            }
        }

        public void Stop()
        {
            lock (m_InitializationStopLock)
            {
                if (State == TickSystemState.Running)
                {
                    StopTicking();
                }

                if (State != TickSystemState.Stopped)
                {
                    HardStop();
                }

                if (m_Drivers.IsCreated)
                {
                    m_Drivers.Dispose();
                    m_Drivers = default;
                }

                (m_ConnectionManager as IDisposable)?.Dispose();
                (m_ConnectionManager as IStateResetable)?.Reset();

                (m_MessageManager as IDisposable)?.Dispose();
                (m_MessageManager as IStateResetable)?.Reset();
            }
        }
    }
}
