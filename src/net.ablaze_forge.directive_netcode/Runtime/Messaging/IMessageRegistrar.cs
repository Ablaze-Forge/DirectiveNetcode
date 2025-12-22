using AblazeForge.DirectiveNetcode.ConnectionData;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public interface IMessageRegistrar<TMessageDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TMessageDelegate : Delegate
        where TConnectionRequirement : IConnectionPermissionRequirement<TPermissions, TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        public ConcurrentDictionary<ushort, MessageDelegateInfo<TMessageDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>> MessageDelegates { get; }

        public bool Register(TMessageDelegate messageDelegate, ushort messageId, TConnectionRequirement connectionRequirement)
        {
            MessageDelegateInfo<TMessageDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue> delegateInfo = new(messageDelegate, connectionRequirement);

            MessageDelegates.TryAdd(messageId, delegateInfo);

            return true;
        }
    }

    public interface IMessageReflectionRegistrar<TMessageAttribute, TMessageDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>
        : IMessageRegistrar<TMessageDelegate, TConnectionRequirement, TPermissions, TPermissionBaseValue>
        where TMessageAttribute : MessageAttributes.MessageDelegateAttributeBase
        where TPermissions : unmanaged, IConnectionPermissions<TPermissionBaseValue>
        where TMessageDelegate : Delegate
        where TConnectionRequirement : IConnectionPermissionRequirement<TPermissions, TPermissionBaseValue>
        where TPermissionBaseValue : unmanaged
    {
        public const string CriteriaFieldName = "PermissionsRequired";

        public TPermissionBaseValue GetRequiredPermissionsFromAttribute(TMessageAttribute attribute)
        {
            Type attributeType = typeof(TMessageAttribute);
            MemberInfo criteriaMember = attributeType.GetField(CriteriaFieldName);
            criteriaMember ??= attributeType.GetProperty(CriteriaFieldName);

            if (criteriaMember == null)
            {
                throw new MissingMemberException($"TMessageAttribute ({attributeType.Name}) must contain a public property or field named 'PermissionsRequired'.");
            }

            object criteriaValueObj = GetMemberValue(criteriaMember, attribute);

            return criteriaValueObj == null
                ? throw new InvalidOperationException($"Could not get value for 'PermissionsRequired' from attribute type {attributeType.Name}.")
                : (TPermissionBaseValue)Convert.ChangeType(criteriaValueObj, typeof(TPermissionBaseValue));
        }

        private static object GetMemberValue(MemberInfo member, TMessageAttribute instance)
        {
            if (member is PropertyInfo property)
            {
                return property.GetValue(instance);
            }
            else if (member is FieldInfo field)
            {
                return field.GetValue(instance);
            }
            return null;
        }

        public int RegisterMessagesViaReflection(Type type)
        {
            int registeredCount = 0;
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (MethodInfo method in methods)
            {
                TMessageAttribute attribute = method.GetCustomAttribute<TMessageAttribute>();

                if (attribute == null) continue;

                TMessageDelegate messageDelegate;

                try
                {
                    messageDelegate = (TMessageDelegate)Delegate.CreateDelegate(typeof(TMessageDelegate), method);
                }
                catch (ArgumentException) { continue; }

                if (messageDelegate == null) continue;

                TPermissionBaseValue requiredPermission;

                try
                {
                    requiredPermission = GetRequiredPermissionsFromAttribute(attribute);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Criteria extraction failed for method {method.Name}: {ex.Message}");
                    continue;
                }

                ushort messageKey = attribute.MessageKey;
                bool needsAuth = attribute.RequiresAuthenticatedState;

                object[] constructorArgs = { requiredPermission, needsAuth };
                TConnectionRequirement connectionRequirement = InstantiateRequirement(needsAuth, requiredPermission);

                if (Register(messageDelegate, messageKey, connectionRequirement))
                {
                    registeredCount++;
                }
            }

            return registeredCount;
        }

        public int RegisterMessagesViaReflection(Assembly assembly)
        {
            int registeredCount = 0;
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                registeredCount += RegisterMessagesViaReflection(type);
            }

            return registeredCount;
        }

        protected TConnectionRequirement InstantiateRequirement(bool needsAuth, TPermissionBaseValue criteria)
        {
            ConstructorInfo connectionRequirementConstructor = typeof(TConnectionRequirement).GetConstructor(new[] { typeof(TPermissionBaseValue), typeof(bool) }) ?? throw new InvalidOperationException(
            $"TConnectionRequirement ({typeof(TConnectionRequirement).Name}) must have a public constructor matching (TCriteria criteria, bool needsToBeAuthenticated) to use the default InstantiateRequirement implementation. Consider overriding InstantiateRequirement for a custom constructor.");
;
            object[] constructorArgs = { criteria, needsAuth };
            TConnectionRequirement connectionRequirement = (TConnectionRequirement)connectionRequirementConstructor.Invoke(constructorArgs);

            return connectionRequirement;
        }
    }
}
