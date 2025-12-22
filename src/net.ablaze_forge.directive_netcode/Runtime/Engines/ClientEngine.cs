using AblazeForge.DirectiveNetcode.Logging;
using AblazeForge.DirectiveNetcode.Messaging;
using System;
using Unity.Collections;
using Unity.Networking.Transport;

namespace AblazeForge.DirectiveNetcode.Engines
{
    public sealed class ClientEngine<TMessageMetadata> : TickSystem
        where TMessageMetadata : unmanaged, IMessageMetadata
    {
        public event Action OnConnected;
        public event Action OnDisonnected;

        private readonly IClientContextSynchronizator m_ContextSynchronizator;
        private readonly object m_InitializationStopLock = new();

        private readonly IClientMessageReceiver m_MessageReceiver;

        private NetworkDriver m_Driver;
        private NetworkConnection m_Connection;

        private readonly IClientMessageManager<TMessageMetadata> m_MessageManager;

        public ClientEngine(IDirectiveNetcodeLogger logger, IClientContextSynchronizator contextSynchronizator, IClientMessageReceiver messageReceiver, IClientMessageManager<TMessageMetadata> messageManager) : base(logger)
        {
            m_ContextSynchronizator = contextSynchronizator;
            m_MessageReceiver = messageReceiver;
            m_MessageManager = messageManager;
        }

        protected override void Tick()
        {
            m_ContextSynchronizator.MoveToStartState();

            m_Driver.ScheduleUpdate().Complete();

            if (!m_Connection.IsCreated)
            {
                return;
            }

            ClientContextSynchronizationState currentState;

            while ((currentState = m_ContextSynchronizator.MoveNext()) != ClientContextSynchronizationState.TickEnded)
            {
                switch (currentState)
                {
                    case ClientContextSynchronizationState.ProcessingMessages:
                        ProcessMessages();
                        break;
                }
            }
        }

        private void ProcessMessages()
        {
            NetworkEvent.Type cmd;

            while ((cmd = m_Connection.PopEvent(m_Driver, out DataStreamReader stream)) != NetworkEvent.Type.Empty)
            {
                switch (cmd)
                {
                    case NetworkEvent.Type.Data:
                        m_MessageReceiver.HandleDataMessage(ref stream);
                        break;
                    case NetworkEvent.Type.Connect:
                        OnConnected?.Invoke();
                        break;
                    case NetworkEvent.Type.Disconnect:
                        OnDisonnected?.Invoke();
                        break;
                }
            }
        }

        public bool Connect(NetworkDriverConfiguration driverConfiguration, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                Logger.Log(GetType().Name, "No valid Ip Address provided, routing to localhost.");

                ipAddress = "127.0.0.1";
            }

            if (m_Driver.IsCreated)
            {
                Logger.LogError(GetType().Name, ErrorCodes.Undefined, "A network driver is already created for this client engine. Call Stop() before attempting to restart a engine.");
            }

            m_Driver = driverConfiguration.GetNetworkDriver();

            var unreliablePipe = m_Driver.CreatePipeline(driverConfiguration.UnreliablePipelineIds.Stages);
            var reliablePipe = m_Driver.CreatePipeline(driverConfiguration.ReliablePipelineIds.Stages);
            var unreliableSequencedPipe = m_Driver.CreatePipeline(driverConfiguration.UnreliableSequencedPipelineIds.Stages);
            var fragmentedPipe = m_Driver.CreatePipeline(driverConfiguration.FragmentationPipelineIds.Stages);

            NetworkEndpoint endpoint = NetworkEndpoint.Parse(ipAddress, driverConfiguration.Port);

            m_Connection = m_Driver.Connect(endpoint);
            m_MessageManager.Initialize(m_Driver, m_Connection, unreliablePipe, reliablePipe, unreliableSequencedPipe, fragmentedPipe);

            return StartTicking();
        }

        public void Stop()
        {
            lock (m_InitializationStopLock)
            {
                if (State == TickSystemState.Running)
                {
                    StopTicking();
                }

                if (State == TickSystemState.Unrecoverable)
                {
                    HardStop();
                }

                if (m_Driver.IsCreated)
                {
                    m_Driver.Dispose();
                }

                if (m_Connection.IsCreated)
                {
                    m_Connection = default;
                }
            }
        }
    }
}
