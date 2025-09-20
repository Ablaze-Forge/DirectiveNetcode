using System;
using System.Collections.Generic;
using System.Reflection;
using AblazeForge.DirectiveNetcode.ConnectionData;
using AblazeForge.DirectiveNetcode.Messaging.MessageAttributes;
using Unity.Collections;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Handles the dispatching of network messages to registered handlers based on message keys.
    /// This class supports both direct delegate handlers and reflection-based handlers for flexible message processing.
    /// </summary>
    public class MessageDispatcher
    {
        /// <summary>
        /// The logger instance used for logging messages and errors from the dispatcher.
        /// </summary>
        private readonly ILogger m_Logger;

        /// <summary>
        /// The message side configuration that determines which handlers are registered during reflection-based registration.
        /// This field is used to filter message handlers based on their <see cref="MessageSide"/> attribute.
        /// </summary>
        private readonly MessageSide m_MessageSide;

        /// <summary>
        /// The connection information center used to retrieve connection details for message dispatching.
        /// </summary>
        private readonly IConnectionInformationProvider m_ConnectionInformationCenter;

        /// <summary>
        /// The dictionary mapping message keys to their registered delegate handlers.
        /// </summary>
        private readonly Dictionary<ushort, DelegateInfo<MessageDelegate>> m_Handlers = new();

        /// <summary>
        /// The dictionary mapping event keys to their registered event delegate handlers.
        /// </summary>
        private readonly Dictionary<ushort, DelegateInfo<EventDelegate>> m_EventHandlers = new();

        /// <summary>
        /// The dictionary mapping control message keys to their registered control message delegate handlers.
        /// </summary>
        private readonly ControlDelegateInfo[] m_ControlHandlers = new ControlDelegateInfo[IConnectionStatus.FlagsSize];

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDispatcher"/> class with the specified logger and message side configuration.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging messages and errors.</param>
        /// <param name="connectionInformationCenter">The <see cref="IConnectionInformationProvider"/> to use as information provider for the connection information.</param>
        /// <param name="side">The message side used to filter which handlers are registered. Only handlers whose <see cref="MessageSide"/> attribute includes this value will be registered. Defaults to <see cref="MessageSide.None"/>, which registers all handlers.</param>
        public MessageDispatcher(ILogger logger, IConnectionInformationProvider connectionInformationCenter, MessageSide side = MessageSide.None)
        {
            m_Logger = logger;
            m_MessageSide = side;
            m_ConnectionInformationCenter = connectionInformationCenter;
        }

        /// <summary>
        /// Registers a delegate handler for messages with the specified key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of message to handle.</param>
        /// <param name="messageDelegate">The delegate handler to register.</param>
        /// <param name="requiredConnectionFlags">The connection flags required for the handler to be invoked. Defaults to 0.</param>
        public void RegisterMessageDelegate(ushort messageKey, MessageDelegate messageDelegate, ushort requiredConnectionFlags = 0)
        {
            RegisterDelegate(messageKey, messageDelegate, m_Handlers, requiredConnectionFlags);
        }

        /// <summary>
        /// Registers a delegate handler for events with the specified key.
        /// </summary>
        /// <param name="eventKey">The key identifying the type of event to handle.</param>
        /// <param name="eventDelegate">The delegate handler to register.</param>
        /// <param name="requiredConnectionFlags">The connection flags required for the handler to be invoked. Defaults to 0.</param>
        public void RegisterEventDelegate(ushort eventKey, EventDelegate eventDelegate, ushort requiredConnectionFlags = 0)
        {
            RegisterDelegate(eventKey, eventDelegate, m_EventHandlers, requiredConnectionFlags);
        }

        /// <summary>
        /// Registers a delegate handler for control messages with the specified key.
        /// </summary>
        /// <param name="controlKey">The key identifying the type of control message to handle.</param>
        /// <param name="messageDelegate">The delegate handler to register.</param>
        /// <param name="requiredConnectionFlags">The connection flags required for the handler to be invoked.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the control key is greater than or equal to <see cref="IConnectionStatus.FlagsSize"/>.</exception>
        public void RegisterControlDelegate(byte controlKey, ushort messageLength, ControlMessageDelegate messageDelegate, ushort requiredConnectionFlags)
        {
            if (controlKey >= IConnectionStatus.FlagsSize)
            {
                throw new ArgumentOutOfRangeException(nameof(controlKey), $"The key of Control messages must be smaller than {IConnectionStatus.FlagsSize}.");
            }

            if (m_ControlHandlers[controlKey] == null)
            {
                m_ControlHandlers[controlKey] = new(messageLength, messageDelegate, requiredConnectionFlags);
            }
            else
            {
                m_ControlHandlers[controlKey] += messageDelegate;
            }
        }

        /// <summary>
        /// Unregisters a delegate handler for messages with the specified key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of message to unregister.</param>
        /// <param name="handler">The delegate handler to unregister.</param>
        public void UnregisterHandler(ushort messageKey, MessageDelegate handler)
        {
            UnregisterDelegate(messageKey, handler, m_Handlers);
        }

        /// <summary>
        /// Unregisters a delegate handler for events with the specified key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of event to unregister.</param>
        /// <param name="handler">The delegate handler to unregister.</param>
        public void UnregisterEventHandler(ushort messageKey, EventDelegate handler)
        {
            UnregisterDelegate(messageKey, handler, m_EventHandlers);
        }

        private void RegisterDelegate<T>(ushort key, T messageDelegate, Dictionary<ushort, DelegateInfo<T>> handlers, ushort requiredConnectionFlags)
            where T : Delegate
        {
            if (handlers.ContainsKey(key))
            {
                handlers[key] += messageDelegate;
            }
            else
            {
                handlers[key] = new(messageDelegate, requiredConnectionFlags);
            }
        }

        private void UnregisterDelegate<T>(ushort key, T messageDelegate, Dictionary<ushort, DelegateInfo<T>> handlers)
            where T : Delegate
        {
            if (handlers.ContainsKey(key))
            {
                handlers[key] -= messageDelegate;

                if (handlers[key].Delegate == null)
                {
                    handlers.Remove(key);
                }
            }
        }

        /// <summary>
        /// Removes all handlers for the specified message key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of message to remove.</param>
        public void RemoveMessage(ushort messageKey)
        {
            m_Handlers.Remove(messageKey);
        }

        /// <summary>
        /// Removes all handlers for the specified event key.
        /// </summary>
        /// <param name="eventKey">The key identifying the type of event to remove.</param>
        public void RemoveEvent(ushort eventKey)
        {
            m_EventHandlers.Remove(eventKey);
        }

        /// <summary>
        /// Removes all handlers for the specified control message key.
        /// </summary>
        /// <param name="controlKey">The key identifying the type of control message to remove.</param>
        public void RemoveControlMessage(ushort controlKey)
        {
            if (controlKey >= m_ControlHandlers.Length)
            {
                return;
            }

            m_ControlHandlers[controlKey] = null;
        }

        /// <summary>
        /// Dispatches a message to the appropriate handler based on the message key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of message to dispatch.</param>
        /// <param name="connectionUID">The unique identifier of the connection that sent the message.</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
        /// <param name="stream">The data stream reader containing the message data.</param>
        public void DispatchMessage(ushort messageKey, ulong connectionUID, MessageMetadataHandler messageMetadata, ref DataStreamReader stream)
        {
            if (TryGetAndValidateDelegate(messageKey, m_Handlers, connectionUID, out DelegateInfo<MessageDelegate> delegateInfo))
            {
                delegateInfo.Delegate?.Invoke(connectionUID, messageMetadata, stream);
            }
        }

        /// <summary>
        /// Invokes an event to the appropriate handler based on the event key.
        /// </summary>
        /// <param name="eventKey">The key identifying the type of event to invoke.</param>
        /// <param name="connectionUID">The unique identifier of the connection that triggered the event.</param>
        /// <param name="messageMetadata">The metadata handler containing information about the event type and characteristics.</param>
        public void InvokeEvent(ushort eventKey, ulong connectionUID, MessageMetadataHandler messageMetadata)
        {
            if (TryGetAndValidateDelegate(eventKey, m_EventHandlers, connectionUID, out DelegateInfo<EventDelegate> delegateInfo))
            {
                delegateInfo.Delegate?.Invoke(connectionUID, messageMetadata);
            }
        }

        /// <summary>
        /// Dispatches a control message to the appropriate handler based on the control message key.
        /// </summary>
        /// <param name="controlKey">The key identifying the type of control message to dispatch.</param>
        /// <param name="connectionUID">The unique identifier of the connection that sent the control message.</param>
        /// <param name="messageMetadata">The metadata handler containing information about the control message type and characteristics.</param>
        /// <param name="stream">The data stream reader containing the control message data.</param>
        public void DispatchControlMessage(ushort controlKey, ulong connectionUID, MessageMetadataHandler messageMetadata, ref DataStreamReader stream)
        {
            if (controlKey >= IConnectionStatus.FlagsSize)
            {
                return;
            }

            ControlDelegateInfo delegateInfo = m_ControlHandlers[controlKey];

            if (delegateInfo == null)
            {
                return;
            }

            if (delegateInfo.MessageLength != stream.Length)
            {
                return;
            }

            if (!ValidateDelegate(connectionUID, delegateInfo.UnderlyingDelegateInfo))
            {
                return;
            }

            bool controlResult = delegateInfo.UnderlyingDelegateInfo.Delegate.Invoke(connectionUID, messageMetadata, stream);

            if (!controlResult)
            {
                return;
            }

            if (!m_ConnectionInformationCenter.GetConnectionInformation(connectionUID, out ConnectionInformation connectionInformation))
            {
                return;
            }

            connectionInformation.Status.SetStatus((byte)controlKey);
        }

        private bool TryGetAndValidateDelegate<T>(ushort key, IReadOnlyDictionary<ushort, DelegateInfo<T>> handlers, ulong connectionUID, out DelegateInfo<T> delegateInfo) where T : Delegate
        {
            if (!handlers.TryGetValue(key, out delegateInfo))
            {
                m_Logger.LogError(GetType().Name, $"Invalid key {key} to handle.");

                return false;
            }

            return ValidateDelegate(connectionUID, delegateInfo);
        }

        private bool ValidateDelegate<T>(ulong connectionUID, DelegateInfo<T> delegateInfo) where T : Delegate
        {
            if (!m_ConnectionInformationCenter.GetConnectionInformation(connectionUID, out ConnectionInformation connectionInformation) || !connectionInformation.Status.MeetsCriteria(delegateInfo.RequiredConnectionFlags))
            {
                m_Logger.Log(GetType().Name, $"Attempt to handle for connection which status does not meet the required flags.");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Registers message handlers via reflection by scanning the specified assembly for methods decorated with
        /// <see cref="MessageAttribute"/> or <see cref="ReflectiveMessageAttribute"/>.
        /// </summary>
        /// <param name="assembly">The assembly to scan for message handlers. If null, uses the calling assembly.</param>
        /// <returns>The number of message handlers successfully registered.</returns>
        /// <remarks>
        /// This method uses the dispatcher's `MessageSide` configuration (set during construction) to filter which handlers are registered.
        /// Only methods with a <see cref="MessageDelegateAttributeBase"/> whose `MessageSide` property contains the dispatcher's configured side will be registered. 
        /// For example, a dispatcher configured with <see cref="MessageSide.Client"/> will register handlers marked with <see cref="MessageSide.Client"/>, <see cref="MessageSide.Common"/>, or <see cref="MessageSide.Any"/>.
        /// </remarks>
        public int RegisterMessagesViaReflection(Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            Type[] types = assembly.GetExportedTypes();

            return RegisterMessagesViaReflection(types);
        }

        /// <summary>
        /// Registers message handlers via reflection by scanning the specified types for methods decorated with
        /// <see cref="MessageAttribute"/> or <see cref="ReflectiveMessageAttribute"/>.
        /// </summary>
        /// <param name="types">The types to scan for message handlers.</param>
        /// <returns>The number of message handlers successfully registered.</returns>
        /// <remarks>
        /// This method uses the dispatcher's `MessageSide` configuration (set during construction) to filter which handlers are registered.
        /// Only methods with a <see cref="MessageDelegateAttributeBase"/> whose `MessageSide` property contains the dispatcher's configured side will be registered.
        /// For example, a dispatcher configured with <see cref="MessageSide.Client"/> will register handlers marked with <see cref="MessageSide.Client"/>, <see cref="MessageSide.Common"/>, or <see cref="MessageSide.Any"/>.
        /// </remarks>
        public int RegisterMessagesViaReflection(params Type[] types)
        {
            int registeredCount = 0;

            foreach (Type type in types)
            {
                registeredCount += RegisterDelegatesForTypeViaReflection(type,
                    (objectType, target, method, throwOnBindFailure) =>
                    {
                        return (MessageDelegate)Delegate.CreateDelegate(typeof(MessageDelegate), target, method, throwOnBindFailure);
                    },
                    WrapAction<MessageDelegate, MessageAttribute>(RegisterMessageDelegate));

                registeredCount += RegisterDelegatesForTypeViaReflection(type,
                    (objectType, target, method, throwOnBindFailure) =>
                    {
                        return (EventDelegate)Delegate.CreateDelegate(typeof(EventDelegate), target, method, throwOnBindFailure);
                    },
                    WrapAction<EventDelegate, EventMessageAttribute>(RegisterEventDelegate));

                registeredCount += RegisterDelegatesForTypeViaReflection<ControlMessageAttribute, ControlMessageDelegate>(type,
                    (objectType, target, method, throwOnBindFailure) =>
                    {
                        return (ControlMessageDelegate)Delegate.CreateDelegate(typeof(ControlMessageDelegate), target, method, throwOnBindFailure);
                    },
                    (controlKey, messageDelegate, requiredConnectionFlags, attribute) =>
                    {
                        RegisterControlDelegate((byte)controlKey, attribute.StreamLength, messageDelegate, requiredConnectionFlags);
                    });
            }

            return registeredCount;

            Action<ushort, TDelegate, ushort, TAttribute> WrapAction<TDelegate, TAttribute>(Action<ushort, TDelegate, ushort> targetMethod)
            {
                return (key, messageDelegate, flags, attribute) => targetMethod(key, messageDelegate, flags);
            }
        }

        private int RegisterDelegatesForTypeViaReflection<TAttribute, TDelegate>(Type type, Func<Type, object, MethodInfo, bool, TDelegate> delegateCreationMethod, Action<ushort, TDelegate, ushort, TAttribute> registerMethod)
            where TAttribute : MessageDelegateAttributeBase
            where TDelegate : Delegate
        {
            int registeredCount = 0;

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<TAttribute>();
                if (attribute != null)
                {
                    try
                    {
                        if (!attribute.MessageSide.HasFlag(m_MessageSide))
                        {
                            continue;
                        }

                        object target = method.IsStatic ? null : Activator.CreateInstance(type);

                        TDelegate handler = delegateCreationMethod(typeof(TDelegate), target, method, false);

                        if (handler != null)
                        {
                            registerMethod(attribute.MessageKey, handler, attribute.RequiredConnectionFlags, attribute);
                            registeredCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(GetType().Name, $"Failed to register handler for method {method.Name} with attribute {typeof(TAttribute).Name}: {ex.Message}");
                    }
                }
            }

            return registeredCount;
        }
    }

    /// <summary>
    /// Represents a delegate for message handlers that process messages with connection information, metadata, and data stream.
    /// </summary>
    /// <param name="connectionUID">The unique identifier of the connection that sent the message.</param>
    /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
    /// <param name="stream">The data stream reader containing the message data.</param>
    public delegate void MessageDelegate(ulong connectionUID, MessageMetadataHandler messageMetadata, DataStreamReader stream);

    /// <summary>
    /// Represents a delegate for event handlers that process events with connection information and metadata.
    /// </summary>
    /// <param name="connectionUID">The unique identifier of the connection that triggered the event.</param>
    /// <param name="messageMetadata">The metadata handler containing information about the event type and characteristics.</param>
    public delegate void EventDelegate(ulong connectionUID, MessageMetadataHandler messageMetadata);

    /// <summary>
    /// Represents a delegate for control message handlers that process control messages with connection information, metadata, and data stream.
    /// </summary>
    /// <param name="connectionUID">The unique identifier of the connection that sent the control message.</param>
    /// <param name="messageMetadata">The metadata handler containing information about the control message type and characteristics.</param>
    /// <param name="stream">The data stream reader containing the control message data.</param>
    /// <returns>A boolean indicating whether the control message was processed successfully.</returns>
    public delegate bool ControlMessageDelegate(ulong connectionUID, MessageMetadataHandler messageMetadata, DataStreamReader stream);

    /// <summary>
    /// Represents information about a delegate handler, including the delegate itself and the required connection flags.
    /// </summary>
    /// <typeparam name="T">The type of the delegate.</typeparam>
    internal sealed class DelegateInfo<T> where T : Delegate
    {
        /// <summary>
        /// Gets the delegate handler.
        /// </summary>
        public T Delegate { get; }

        /// <summary>
        /// Gets the required connection flags for the delegate to be invoked.
        /// </summary>
        public ushort RequiredConnectionFlags { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateInfo{T}"/> class.
        /// </summary>
        /// <param name="messageDelegate">The delegate handler.</param>
        /// <param name="flags">The required connection flags.</param>
        public DelegateInfo(T messageDelegate, ushort flags)
        {
            Delegate = messageDelegate;
            RequiredConnectionFlags = flags;
        }

        /// <summary>
        /// Combines the delegate with another delegate.
        /// </summary>
        /// <param name="messageDelegateInfo">The delegate info.</param>
        /// <param name="delegateToAdd">The delegate to add.</param>
        /// <returns>A new <see cref="DelegateInfo{T}"/> with the combined delegate.</returns>
        public static DelegateInfo<T> operator +(DelegateInfo<T> messageDelegateInfo, T delegateToAdd)
        {
            T messageDelegate = messageDelegateInfo.Delegate;

            messageDelegate = (T)System.Delegate.Combine(messageDelegate, delegateToAdd);

            return new(messageDelegate, messageDelegateInfo.RequiredConnectionFlags);
        }

        /// <summary>
        /// Removes a delegate from the delegate info.
        /// </summary>
        /// <param name="messageDelegateInfo">The delegate info.</param>
        /// <param name="delegateToRemove">The delegate to remove.</param>
        /// <returns>A new <see cref="DelegateInfo{T}"/> with the removed delegate.</returns>
        public static DelegateInfo<T> operator -(DelegateInfo<T> messageDelegateInfo, T delegateToRemove)
        {
            T messageDelegate = messageDelegateInfo.Delegate;

            messageDelegate = (T)System.Delegate.Remove(messageDelegate, delegateToRemove);

            return new(messageDelegate, messageDelegateInfo.RequiredConnectionFlags);
        }
    }

    /// <summary>
    /// Represents information about a control message delegate handler, including the underlying delegate info and message length.
    /// </summary>
    internal sealed class ControlDelegateInfo
    {
        /// <summary>
        /// Gets the underlying delegate information for the control message handler.
        /// </summary>
        public DelegateInfo<ControlMessageDelegate> UnderlyingDelegateInfo { get; }

        /// <summary>
        /// Gets the expected length of the control message.
        /// </summary>
        public ushort MessageLength { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlDelegateInfo"/> class.
        /// </summary>
        /// <param name="messageLength">The expected length of the control message.</param>
        /// <param name="messageDelegate">The control message delegate.</param>
        /// <param name="flags">The required connection flags.</param>
        public ControlDelegateInfo(ushort messageLength, ControlMessageDelegate messageDelegate, ushort flags)
        {
            UnderlyingDelegateInfo = new(messageDelegate, flags);
            MessageLength = messageLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlDelegateInfo"/> class with existing delegate info.
        /// </summary>
        /// <param name="messageLength">The expected length of the control message.</param>
        /// <param name="delegateInfo">The delegate information.</param>
        private ControlDelegateInfo(ushort messageLength, DelegateInfo<ControlMessageDelegate> delegateInfo)
        {
            MessageLength = messageLength;
            UnderlyingDelegateInfo = delegateInfo;
        }

        /// <summary>
        /// Adds a delegate to the control delegate info.
        /// </summary>
        /// <param name="controlDelegateInfo">The control delegate info.</param>
        /// <param name="delegateToAdd">The delegate to add.</param>
        /// <returns>A new <see cref="ControlDelegateInfo"/> with the added delegate.</returns>
        public static ControlDelegateInfo operator +(ControlDelegateInfo controlDelegateInfo, ControlMessageDelegate delegateToAdd)
        {
            DelegateInfo<ControlMessageDelegate> delegateInfo = controlDelegateInfo.UnderlyingDelegateInfo;

            delegateInfo += delegateToAdd;

            return new(controlDelegateInfo.MessageLength, delegateInfo);
        }

        /// <summary>
        /// Removes a delegate from the control delegate info.
        /// </summary>
        /// <param name="controlDelegateInfo">The control delegate info.</param>
        /// <param name="delegateToRemove">The delegate to remove.</param>
        /// <returns>A new <see cref="ControlDelegateInfo"/> with the removed delegate.</returns>
        public static ControlDelegateInfo operator -(ControlDelegateInfo controlDelegateInfo, ControlMessageDelegate delegateToRemove)
        {
            DelegateInfo<ControlMessageDelegate> delegateInfo = controlDelegateInfo.UnderlyingDelegateInfo;

            delegateInfo -= delegateToRemove;

            return new(controlDelegateInfo.MessageLength, delegateInfo);
        }
    }
}
