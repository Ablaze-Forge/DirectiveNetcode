using AblazeForge.DirectiveNetcode.ConnectionData;
using AblazeForge.DirectiveNetcode.Utilities;
using System;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public class MessageDelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>
        where TDelegate : Delegate
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TConnectionRequirement : IConnectionPermissionRequirement<TPermissions, TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        public TDelegate Delegate { get; private set; }
        public TConnectionRequirement ConnectionRequirement { get; private set; }

        public MessageDelegateInfo(TDelegate @delegate, TConnectionRequirement connectionRequirement)
        {
            Delegate = @delegate;
            ConnectionRequirement = connectionRequirement;
        }

        public static MessageDelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue> operator
            +(MessageDelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue> info,
            MessageDelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue> other)
        {
            TDelegate resultingDelegate = (TDelegate)System.Delegate.Combine(info.Delegate, other.Delegate);
            TConnectionRequirement resultingRequirement;

            if (info.ConnectionRequirement != null && other.ConnectionRequirement != null
                && info.ConnectionRequirement is ICombinable<TConnectionRequirement> combinableInfo)
            {
                resultingRequirement = combinableInfo.Combine(other.ConnectionRequirement);
            }
            else
            {
                resultingRequirement = info.ConnectionRequirement ?? other.ConnectionRequirement ?? default;
            }

            return new(resultingDelegate, resultingRequirement);
        }

        public static MessageDelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue> operator
            +(MessageDelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue> info, TDelegate value)
        {
            TDelegate resultingDelegate = (TDelegate)System.Delegate.Combine(info.Delegate, value);

            return new MessageDelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>(resultingDelegate, info.ConnectionRequirement);
        }

        public static MessageDelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue> operator
            -(MessageDelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue> info, TDelegate value)
        {
            TDelegate resultingDelegate = (TDelegate)System.Delegate.Remove(info.Delegate, value);

            return new MessageDelegateInfo<TDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>(resultingDelegate, info.ConnectionRequirement);
        }
    }
}
