using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Networking.Transport;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public interface IServerMessageManager<TId, TMetadata>
        where TId : unmanaged, IEquatable<TId>
        where TMetadata : unmanaged, IMessageMetadata
    {
        public bool Initialize(MultiNetworkDriver drivers, params NetworkPipeline[] pipelines);
        public ValueTask<(bool result, DataStreamWriter writer)> CreateMessageAsync(MessageSendType messageSendType, ushort messageId, TMetadata metadata, TId connectionId);
        public IEnumerator CreateMessageCoroutine(NetworkPipeline pipeline, ushort messageId, TMetadata metadata, TId connectionId);
        public IEnumerator CreateMessageCoroutine(NetworkPipeline pipeline, ushort messageId, TMetadata metadata, TId connectionId, Action<DataStreamWriter> actionOnSuccess, Action actionOnFailure);
        public bool Send(DataStreamWriter writer);
    }

    public interface IClientMessageManager<TMetadata>
        where TMetadata : unmanaged, IMessageMetadata
    {
        public bool Initialize(NetworkDriver driver, NetworkConnection connection, params NetworkPipeline[] pipelines);
        public ValueTask<(bool result, DataStreamWriter writer)> CreateMessageAsync(MessageSendType messageSendType, ushort messageId, TMetadata metadata);
        public IEnumerator CreateMessageCoroutine(MessageSendType messageSendType, ushort messageId, TMetadata metadata);
        public IEnumerator CreateMessageCoroutine(MessageSendType messageSendType, ushort messageId, TMetadata metadata, Action<DataStreamWriter> actionOnSuccess, Action actionOnFailure);
        public bool Send(DataStreamWriter writer);
    }
}
