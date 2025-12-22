using System;
using Unity.Collections;
using Unity.Networking.Transport;

namespace AblazeForge.DirectiveNetcode.ConnectionData
{
    public interface IServerConnectionManager<TId, TPermissions, TConnectionInformation, TPermissionBaseValue>
        where TId : unmanaged, IEquatable<TId>
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        public bool Initialize(int maxConnectionCount);
        public int CleanInnactiveConnections();
        public bool AddConnection(NetworkConnection connection, out TId connectionId);
        public bool RemoveConnection(TId connectionId, out NetworkConnection connection);
        public NativeArray<TId> GetAllConnectionIds(Allocator allocator = Allocator.Temp);
        public NetworkConnection GetNetworkConnection(TId connectionId);

        public bool TryGetConnectionInformation(TId connectionId, out TConnectionInformation connectionInformation);
    }

    public interface IClientConnectionManager
    {

    }
}
