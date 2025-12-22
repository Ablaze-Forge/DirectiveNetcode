using AblazeForge.DirectiveNetcode.ConnectionIdProviders;
using AblazeForge.DirectiveNetcode.Utilities;
using System;
using Unity.Collections;
using Unity.Networking.Transport;

namespace AblazeForge.DirectiveNetcode.ConnectionData
{
    public class DefaultServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue> : IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>, IStateResetable
        where TId : unmanaged, IEquatable<TId>
        where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        public event Action<NetworkConnection, ConnectionRejectReason> OnNewConnectionRejected;
        public event Action<TId> OnNewConnectionAccepted;

        private readonly IConnectionIdProvider<TId> m_ConnectionIdProvider;
        private readonly IConnectionFilter m_ConnectionFilter;

        private NativeHashMap<TId, NetworkConnection> m_Connections;
        private NativeHashMap<TId, TConnectionInformation> m_ConnectionInformation;
        private int m_MaxConnectionCount;
        private bool m_IsReadyToStart = true;

        public DefaultServerConnectionManager(IConnectionIdProvider<TId> connectionIdProvider, IConnectionFilter connectionFilter)
        {
            m_ConnectionIdProvider = connectionIdProvider;
            m_ConnectionFilter = connectionFilter ?? new NoOpConnectionFilter();
        }

        public bool Initialize(int maxConnectionCount)
        {
            if (!m_IsReadyToStart)
            {
                return false;
            }

            m_MaxConnectionCount = maxConnectionCount;

            int initialCapacity = Math.Min(16, maxConnectionCount);

            m_Connections = new NativeHashMap<TId, NetworkConnection>(initialCapacity, Allocator.Persistent);
            m_ConnectionInformation = new NativeHashMap<TId, TConnectionInformation>(initialCapacity, Allocator.Persistent);

            return true;
        }

        public int CleanInnactiveConnections()
        {
            using NativeArray<TId> keyArray = GetAllConnectionIds();
            int removedCount = 0;

            if (keyArray.Length == 0)
            {
                return 0;
            }

            for (int i = keyArray.Length - 1; i >= 0; i--)
            {
                TId connectionId = keyArray[i];

                NetworkConnection connection = GetNetworkConnection(connectionId);

                if (!connection.IsCreated)
                {
                    RemoveConnection(connectionId, out _);
                    removedCount++;
                }
            }

            return removedCount;
        }

        public bool AddConnection(NetworkConnection connection, out TId connectionId)
        {
            connectionId = default;

            if (m_Connections.Count >= m_MaxConnectionCount)
            {
                OnNewConnectionRejected?.Invoke(connection, ConnectionRejectReason.MaxConnectionsReached);
                return false;
            }

            if (!m_ConnectionFilter.ShouldAllowConnection(connection))
            {
                OnNewConnectionRejected?.Invoke(connection, ConnectionRejectReason.FilterRejected);
                return false;
            }

            connectionId = m_ConnectionIdProvider.GenerateNext();

            if (!m_Connections.TryAdd(connectionId, connection))
            {
                return false;
            }

            TConnectionInformation connectionInfo = default;
            connectionInfo.SetId(connectionId);

            if (!m_ConnectionInformation.TryAdd(connectionId, connectionInfo))
            {
                m_Connections.Remove(connectionId);
                return false;
            }

            OnNewConnectionAccepted?.Invoke(connectionId);

            return true;
        }

        public bool RemoveConnection(TId connectionId, out NetworkConnection connection)
        {
            if (m_Connections.ContainsKey(connectionId))
            {
                connection = m_Connections[connectionId];

                return m_Connections.Remove(connectionId);
            }

            connection = default;
            return false;
        }

        public NativeArray<TId> GetAllConnectionIds(Allocator allocator = Allocator.Temp)
        {
            return m_Connections.GetKeyArray(allocator);
        }

        public NetworkConnection GetNetworkConnection(TId connectionId)
        {
            if (m_Connections.TryGetValue(connectionId, out NetworkConnection connection))
            {
                return connection;
            }

            return default;
        }

        public void Reset()
        {
            m_Connections.Dispose();
            m_MaxConnectionCount = 0;

            m_IsReadyToStart = true;
        }

        public bool TryGetConnectionInformation(TId connectionId, out TConnectionInformation connectionInformation)
        {
            return m_ConnectionInformation.TryGetValue(connectionId, out connectionInformation);
        }

        public enum ConnectionRejectReason
        {
            MaxConnectionsReached,
            FilterRejected,
        }
    }
}
