using AblazeForge.DirectiveNetcode.ConnectionData;
using AblazeForge.DirectiveNetcode.Engines;
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public class ServerMessageManager<TId, TMetadata, TPermissions, TConnectionInformation, TPermissionBaseValue> : IServerMessageManager<TId, TMetadata>
        where TId : unmanaged, IEquatable<TId>
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
        where TMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
        where TPermissionBaseValue : unmanaged
    {
        private readonly IServerContextSynchronizator m_Synchronizator;
        private readonly IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> m_ConnectionManager;
        private readonly IServerMessageSender<TId, TMetadata> m_MessageSender;

        private NativeArray<NetworkPipeline> m_Pipelines;

        private MultiNetworkDriver m_Drivers;

        public ServerMessageManager(IServerContextSynchronizator contextSynchronizator, IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> connectionManager, IServerMessageSender<TId, TMetadata> messageSender)
        {
            m_Synchronizator = contextSynchronizator;
            m_ConnectionManager = connectionManager;
            m_MessageSender = messageSender;
        }

        public bool Initialize(MultiNetworkDriver drivers, params NetworkPipeline[] pipelines)
        {
            if (!drivers.IsCreated)
            {
                return false;
            }

            m_Drivers = drivers;

            m_Pipelines = new(pipelines, Allocator.Persistent);

            return true;
        }

        public async ValueTask<(bool result, DataStreamWriter writer)> CreateMessageAsync(MessageSendType sendType, ushort messageId, TMetadata metadata, TId connectionId)
        {
            CheckIfInitialized();

            if (m_Synchronizator.State == ServerContextSynchronizationState.NoSynchronizationAvailable)
            {
                return (false, default);
            }

            if (m_Synchronizator.State != ServerContextSynchronizationState.TickEnded)
            {
                m_Synchronizator.HasListenerForProcessingEnd = true;
                await m_Synchronizator.ProcessingTaskCompletionSource.Task;
            }

            NetworkConnection connection = m_ConnectionManager.GetNetworkConnection(connectionId);

            if (!connection.IsCreated)
            {
                return (false, default);
            }

            if (m_Drivers.BeginSend(m_Pipelines[(int)sendType], connection, out DataStreamWriter writer) != 0)
            {
                return (false, default);
            }

            if(m_MessageSender.PrepareMessage(connectionId, messageId, metadata, ref writer) != MessagePreparationResult.Success)
            {
                m_Drivers.AbortSend(writer);
                return (false, default);
            }

            return (true, writer);
        }

        public IEnumerator CreateMessageCoroutine(NetworkPipeline pipeline, ushort messageId, TMetadata metadata, TId connectionId)
        {
            CheckIfInitialized();

            yield return new WaitUntil(() => m_Synchronizator.State == ServerContextSynchronizationState.TickEnded);

            if (m_Synchronizator.State != ServerContextSynchronizationState.TickEnded)
            {
                yield return default(DataStreamWriter);
                yield break;
            }

            NetworkConnection connection = m_ConnectionManager.GetNetworkConnection(connectionId);

            if (!connection.IsCreated)
            {
                yield return default(DataStreamWriter);
                yield break;
            }

            if (m_Drivers.BeginSend(pipeline, connection, out DataStreamWriter writer) != 0)
            {
                yield return default(DataStreamWriter);
                yield break;
            }

            if (m_MessageSender.PrepareMessage(connectionId, messageId, metadata, ref writer) != MessagePreparationResult.Success)
            {
                m_Drivers.AbortSend(writer);
                yield return default(DataStreamWriter);
                yield break;
            }

            yield return writer;
        }

        public IEnumerator CreateMessageCoroutine(NetworkPipeline pipeline, ushort messageId, TMetadata metadata, TId connectionId, Action<DataStreamWriter> actionOnSuccess, Action actionOnFailure)
        {
            CheckIfInitialized();

            yield return new WaitUntil(() => m_Synchronizator.State == ServerContextSynchronizationState.TickEnded);

            if (m_Synchronizator.State != ServerContextSynchronizationState.TickEnded)
            {
                actionOnFailure?.Invoke();
                yield break;
            }

            NetworkConnection connection = m_ConnectionManager.GetNetworkConnection(connectionId);

            if (!connection.IsCreated)
            {
                actionOnFailure?.Invoke();
                yield break;
            }

            if (m_Drivers.BeginSend(pipeline, connection, out DataStreamWriter writer) != 0)
            {
                actionOnFailure?.Invoke();
                yield break;
            }

            if (m_MessageSender.PrepareMessage(connectionId, messageId, metadata, ref writer) != MessagePreparationResult.Success)
            {
                m_Drivers.AbortSend(writer);
                actionOnFailure?.Invoke();
                yield break;
            }

            actionOnSuccess?.Invoke(writer);
        }

        public bool Send(DataStreamWriter writer)
        {
            CheckIfInitialized();

            if (!writer.IsCreated)
            {
                return false;
            }

            m_Drivers.EndSend(writer);

            return true;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private void CheckIfInitialized()
        {
            if (!m_Drivers.IsCreated)
            {
                throw new InvalidOperationException("Message Manager is not initialized yet, make sure to call it only AFTER ServerEngine.Start is called");
            }
        }
    }
}
