using AblazeForge.DirectiveNetcode.ConnectionData;
using System;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public readonly struct DelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>
        where TDelegate : Delegate
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
        where TConnectionRequirement : IConnectionPermissionRequirement<TPermissions,TPermissionBaseValue>
    {
        public TDelegate Delegate { get; }

        public TConnectionRequirement RequiredPermissions { get; }

        public DelegateInfo(TDelegate @delegate, TConnectionRequirement requiredPermissions)
        {
            Delegate = @delegate;
            RequiredPermissions = requiredPermissions;
        }

        public static DelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>
            operator +(DelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue> info, TDelegate delegateToAdd)
        {
            TDelegate newDelegate = (TDelegate)System.Delegate.Combine(info.Delegate, delegateToAdd);
            return new(newDelegate, info.RequiredPermissions);
        }

        public static DelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>
            operator -(DelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue> info, TDelegate delegateToRemove)
        {
            TDelegate newDelegate = (TDelegate)System.Delegate.Remove(info.Delegate, delegateToRemove);
            return new(newDelegate, info.RequiredPermissions);
        }
    }
}
