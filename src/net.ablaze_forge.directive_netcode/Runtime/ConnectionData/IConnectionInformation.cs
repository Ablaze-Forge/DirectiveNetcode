using System;

namespace AblazeForge.DirectiveNetcode.ConnectionData
{
    public interface IConnectionInformation<TId, TPermissions, TPermissionBaseValue>
        where TId : unmanaged, IEquatable<TId>
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        public TId Id { get; }
        public TPermissions Permissions { get; }
        public bool IsAuthenticated { get; }

        public void SetId(TId id);
    }

    public struct DefaultConnectionInformation<TId> : IConnectionInformation<TId, UshortConnectionPermissions, ushort>
        where TId : unmanaged, IEquatable<TId>
    {
        public readonly TId Id => m_Id;
        private TId m_Id;
        public UshortConnectionPermissions Permissions { get; set; }
        public bool IsAuthenticated { get; set; }

        public void SetId(TId id)
        {
            m_Id = id;
        }
    }
}
