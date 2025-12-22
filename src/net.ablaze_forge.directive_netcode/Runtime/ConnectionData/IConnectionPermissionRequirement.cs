using System;

namespace AblazeForge.DirectiveNetcode.ConnectionData
{
    public interface IConnectionPermissionRequirement<TPermissions, TPermissionBaseValue>
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        public bool NeedsToBeAuthenticated { get; }
        public TPermissions PermissionsRequired { get; }
        public bool MeetsCriteria<TId, TConnectionInformation>(TConnectionInformation connectionInformation)
            where TId : unmanaged, IEquatable<TId>
            where TConnectionInformation : unmanaged, IConnectionInformation<TId, TPermissions, TPermissionBaseValue>;
    }

    public class DefaultConnectionPermissionRequirement : IConnectionPermissionRequirement<UshortConnectionPermissions, ushort>
    {
        public DefaultConnectionPermissionRequirement(ushort permissionsRequired, bool needsToBeAuthenticated)
        {
            PermissionsRequired = permissionsRequired;
            NeedsToBeAuthenticated = needsToBeAuthenticated;
        }

        public bool NeedsToBeAuthenticated { get; private set; }
        public ushort PermissionsRequired { get; private set; }

        UshortConnectionPermissions IConnectionPermissionRequirement<UshortConnectionPermissions, ushort>.PermissionsRequired => new(PermissionsRequired);

        public bool MeetsCriteria<TId, TConnectionInformation>(TConnectionInformation connectionInformation)
            where TId : unmanaged, IEquatable<TId>
            where TConnectionInformation : unmanaged, IConnectionInformation<TId, UshortConnectionPermissions, ushort>
        {
            if ((connectionInformation.Permissions.BaseValue & PermissionsRequired) != PermissionsRequired)
            {
                return false;
            }

            if (NeedsToBeAuthenticated)
            {
                return connectionInformation.IsAuthenticated;
            }

            return true;
        }
    }
}
