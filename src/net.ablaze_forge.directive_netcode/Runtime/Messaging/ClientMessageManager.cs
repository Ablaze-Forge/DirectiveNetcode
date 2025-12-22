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
    public class ClientMessageManager<TMetadata> : IClientMessageManager<TMetadata>
        where TMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
    {
        private readonly IClientContextSynchronizator m_Synchronizator;
        private readonly IClientMessageSender<TMetadata> m_MessageSender;

        private NetworkDriver m_Driver;
        private NetworkConnection m_Connection;
        private NativeArray<NetworkPipeline> m_Pipelines;

        public ClientMessageManager(IClientContextSynchronizator contextSynchronizator, IClientMessageSender<TMetadata> messageSender)
        {
            m_Synchronizator = contextSynchronizator;
            m_MessageSender = messageSender;
        }

        public async ValueTask<(bool result, DataStreamWriter writer)> CreateMessageAsync(MessageSendType messageSendType, ushort messageId, TMetadata metadata)
        {
            CheckIfInitialized();

            if(m_Synchronizator.State != ClientContextSynchronizationState.TickEnded)
            {
                m_Synchronizator.HasListenerForProcessingEnd = true;
                await m_Synchronizator.ProcessingTaskCompletionSource.Task;
            }

            if (!m_Connection.IsCreated)
            {
                UnityEngine.Debug.Log("Connection is not created yet.");
                return (false, default);
            }

            var pipeline = m_Pipelines[(int)messageSendType];

            if (m_Driver.BeginSend(pipeline, m_Connection, out DataStreamWriter writer) != 0)
            {
                UnityEngine.Debug.Log("Send call failed.");
                return (false, default);
            }

            if (m_MessageSender.PrepareMessage(messageId, metadata, ref writer) != MessagePreparationResult.Success)
            {
                UnityEngine.Debug.Log("Preparation Failed.");
                return (false, default);
            }

            return (true, writer);
        }

        public IEnumerator CreateMessageCoroutine(MessageSendType messageSendType, ushort messageId, TMetadata metadata)
        {
            CheckIfInitialized();

            yield return new WaitUntil(() => m_Synchronizator.State == ClientContextSynchronizationState.TickEnded);

            if (m_Synchronizator.State != ClientContextSynchronizationState.TickEnded)
            {
                yield return default(DataStreamWriter);
                yield break;
            }

            if (!m_Connection.IsCreated)
            {
                yield return default(DataStreamWriter);
                yield break;
            }

            var pipeline = m_Pipelines[(int)messageSendType];

            if (m_Driver.BeginSend(pipeline, m_Connection, out DataStreamWriter writer) != 0)
            {
                yield return default(DataStreamWriter);
                yield break;
            }

            if (m_MessageSender.PrepareMessage(messageId, metadata, ref writer) != MessagePreparationResult.Success)
            {
                m_Driver.AbortSend(writer);
                yield return default(DataStreamWriter);
                yield break;
            }

            yield return writer;
        }

        public IEnumerator CreateMessageCoroutine(MessageSendType messageSendType, ushort messageId, TMetadata metadata, Action<DataStreamWriter> actionOnSuccess, Action actionOnFailure)
        {
            CheckIfInitialized();

            yield return new WaitUntil(() => m_Synchronizator.State == ClientContextSynchronizationState.TickEnded);

            if (m_Synchronizator.State != ClientContextSynchronizationState.TickEnded)
            {
                actionOnFailure?.Invoke();
                yield break;
            }

            if (!m_Connection.IsCreated)
            {
                actionOnFailure?.Invoke();
                yield break;
            }

            var pipeline = m_Pipelines[(int)messageSendType];

            if (m_Driver.BeginSend(pipeline, m_Connection, out DataStreamWriter writer) != 0)
            {
                actionOnFailure?.Invoke();
                yield break;
            }

            if (m_MessageSender.PrepareMessage(messageId, metadata, ref writer) != MessagePreparationResult.Success)
            {
                m_Driver.AbortSend(writer);
                actionOnFailure?.Invoke();
                yield break;
            }

            actionOnSuccess?.Invoke(writer);
        }

        public bool Initialize(NetworkDriver driver, NetworkConnection connection, params NetworkPipeline[] pipelines)
        {
            if (!driver.IsCreated)
            {
                return false;
            }

            m_Driver = driver;
            m_Connection = connection;

            m_Pipelines = new(pipelines, Allocator.Persistent);

            return true;
        }

        public bool Send(DataStreamWriter writer)
        {
            CheckIfInitialized();

            if (!writer.IsCreated)
            {
                return false;
            }

            return m_Driver.EndSend(writer) == 0;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private void CheckIfInitialized()
        {
            if (!m_Driver.IsCreated)
            {
                throw new InvalidOperationException("Message Manager is not initialized yet, make sure to call it only AFTER ServerEngine.Start is called");
            }
        }
    }
}
