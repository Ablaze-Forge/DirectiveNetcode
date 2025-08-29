using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using AblazeForge.DirectiveNetcode.Unity.Extensions;
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
        /// Initializes a new instance of the <see cref="MessageDispatcher"/> class with the specified logger and message side configuration.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging messages and errors.</param>
        /// <param name="side">The message side used to filter which handlers are registered. Only handlers whose <see cref="MessageSide"/> attribute includes this value will be registered. Defaults to <see cref="MessageSide.None"/>, which registers all handlers.</param>
        public MessageDispatcher(ILogger logger, MessageSide side = MessageSide.None)
        {
            m_Logger = logger;
            m_MessageSide = side;
        }

        /// <summary>
        /// The dictionary mapping message keys to their registered delegate handlers.
        /// </summary>
        private readonly Dictionary<ushort, MessageDelegate> m_Handlers = new();

        /// <summary>
        /// Registers a delegate handler for messages with the specified key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of message to handle.</param>
        /// <param name="handler">The delegate handler to register.</param>
        public void RegisterHandler(ushort messageKey, MessageDelegate handler)
        {
            if (m_Handlers.ContainsKey(messageKey))
            {
                m_Handlers[messageKey] += handler;
            }
            else
            {
                m_Handlers[messageKey] = handler;
            }
        }

        /// <summary>
        /// Registers a reflection-based handler for messages with the specified key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of message to handle.</param>
        /// <param name="target">The target object instance for the method, or null for static methods.</param>
        /// <param name="method">The method information for the handler method.</param>
        public void RegisterReflectionHandler(ushort messageKey, object target, MethodInfo method)
        {
            try
            {
                MessageDelegate handler = MessageHandlerReflectionFactory.CreateDelegate(target, method);

                RegisterHandler(messageKey, handler);
            }
            catch (ArgumentException ex)
            {
                m_Logger.LogError(GetType().Name, $"Failed to register reflection handler for message key {messageKey}: {ex.Message}");
            }
        }

        /// <summary>
        /// Unregisters a delegate handler for messages with the specified key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of message to unregister.</param>
        /// <param name="handler">The delegate handler to unregister.</param>
        public void UnregisterHandler(ushort messageKey, MessageDelegate handler)
        {
            if (m_Handlers.ContainsKey(messageKey))
            {
                m_Handlers[messageKey] -= handler;

                if (m_Handlers[messageKey] == null)
                {
                    m_Handlers.Remove(messageKey);
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
        /// Dispatches a message to the appropriate handler based on the message key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of message to dispatch.</param>
        /// <param name="connectionUID">The unique identifier of the connection that sent the message.</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
        /// <param name="stream">The data stream reader containing the message data.</param>
        public void DispatchMessage(ushort messageKey, ulong connectionUID, MessageMetadataHandler messageMetadata, ref DataStreamReader stream)
        {
            if (m_Handlers.TryGetValue(messageKey, out var handler))
            {
                handler?.Invoke(connectionUID, messageMetadata, stream);
                return;
            }
            else
            {
                m_Logger.LogError(GetType().Name, $"Invalid message key {messageKey} to dispatch.");
            }
        }

        /// <summary>
        /// Registers message handlers via reflection by scanning the specified assembly for methods decorated with
        /// <see cref="MessageHandlerAttribute"/> or <see cref="MessageReflectionHandlerAttribute"/>.
        /// </summary>
        /// <param name="assembly">The assembly to scan for message handlers. If null, uses the calling assembly.</param>
        /// <returns>The number of message handlers successfully registered.</returns>
        /// <remarks>
        /// This method uses the dispatcher's `MessageSide` configuration (set during construction) to filter which handlers are registered.
        /// Only methods with a <see cref="MessageHandlerAttribute"/> or <see cref="MessageReflectionHandlerAttribute"/> whose `MessageSide` property contains the dispatcher's configured side will be registered. 
        /// For example, a dispatcher configured with <see cref="MessageSide.Client"/> will register handlers marked with <see cref="MessageSide.Client"/>, <see cref="MessageSide.Common"/>, or <see cref="MessageSide.Any"/>.
        /// </remarks>
        public int RegisterMessagesViaReflection(Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            int registeredCount = 0;

            Type[] types = assembly.GetExportedTypes();

            foreach (Type type in types)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                foreach (var method in methods)
                {
                    var messageHandlerAttribute = method.GetCustomAttribute<MessageHandlerAttribute>();
                    if (messageHandlerAttribute != null)
                    {
                        try
                        {
                            if (!messageHandlerAttribute.MessageSide.HasFlag(m_MessageSide))
                            {
                                continue;
                            }

                            object target = method.IsStatic ? null : Activator.CreateInstance(type);
                            MessageDelegate handler = (MessageDelegate)Delegate.CreateDelegate(typeof(MessageDelegate), target, method, false);

                            if (handler != null)
                            {
                                RegisterHandler(messageHandlerAttribute.MessageKey, handler);
                                registeredCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            m_Logger.LogError(GetType().Name, $"Failed to register direct handler for method {method.Name}: {ex.Message}");
                        }
                    }

                    var reflectionHandlerAttribute = method.GetCustomAttribute<MessageReflectionHandlerAttribute>();
                    if (reflectionHandlerAttribute != null)
                    {
                        try
                        {
                            if (!messageHandlerAttribute.MessageSide.HasFlag(m_MessageSide))
                            {
                                continue;
                            }

                            object target = method.IsStatic ? null : Activator.CreateInstance(type);
                            RegisterReflectionHandler(reflectionHandlerAttribute.MessageKey, target, method);
                            registeredCount++;
                        }
                        catch (Exception ex)
                        {
                            m_Logger.LogError(GetType().Name, $"Failed to register reflection handler for method {method.Name}: {ex.Message}");
                        }
                    }
                }
            }

            return registeredCount;
        }

        /// <summary>
        /// A factory class that uses reflection to create <see cref="MessageDelegate"/> delegates.
        /// </summary>
        /// <remarks>
        /// This class dynamically compiles an expression tree to create a high-performance delegate that can handle incoming messages based on a provided method's signature.
        /// It supports methods with parameters that can be deserialized from a data stream, as well as special parameters for connection UID and message metadata.
        /// </remarks>
        private static class MessageHandlerReflectionFactory
        {
            /// <summary>
            /// The name of the connection UID parameter.
            /// </summary>
            private const string connectionUIDParameterName = "connectionUID";

            /// <summary>
            /// The name of the message metadata parameter.
            /// </summary>
            private const string messageMetadataParameterName = "messageMetadata";

            /// <summary>
            /// Creates a <see cref="MessageDelegate"/> for the method provided.
            /// </summary>
            /// <param name="target">The target object instance for the method, or null for static methods.</param>
            /// <param name="method">The method information for the handler method.</param>
            /// <exception cref="ArgumentException">Thrown when target is null for non-static methods or when no deserializer is found for a parameter type.</exception>
            /// <returns>The compiled <see cref="MessageDelegate"/> delegate</returns>
            public static MessageDelegate CreateDelegate(object target, MethodInfo method)
            {
                if (!method.IsStatic && target == null)
                {
                    throw new ArgumentException("Target cannot be null for non-static methods.", nameof(target));
                }

                if (method.IsStatic && target != null)
                {
                    target = null;
                }

                ParameterInfo[] subscriberMethodParameters = method.GetParameters();

                string[] parameterNames = new string[subscriberMethodParameters.Length];

                Deserializers.Deserializer[] cachedDeserializers = new Deserializers.Deserializer[subscriberMethodParameters.Length];

                var returnLabel = Expression.Label("returnFromHandler");

                var connectionUIDParam = Expression.Parameter(typeof(ulong), connectionUIDParameterName);
                var messageMetadataParam = Expression.Parameter(typeof(MessageMetadataHandler), messageMetadataParameterName);

                var streamParam = Expression.Parameter(typeof(DataStreamReader).MakeByRefType(), "stream");

                var methodCallArgs = new List<Expression>();
                var blockExpressions = new List<Expression>();
                var localVariables = new List<ParameterExpression>();

                for (int i = 0; i < subscriberMethodParameters.Length; i++)
                {
                    var param = subscriberMethodParameters[i];
                    parameterNames[i] = param.Name;

                    if (param.Name == messageMetadataParameterName)
                    {
                        methodCallArgs.Add(messageMetadataParam);
                        cachedDeserializers[i] = null;
                    }
                    else if (param.Name == connectionUIDParameterName)
                    {
                        methodCallArgs.Add(connectionUIDParam);
                        cachedDeserializers[i] = null;
                    }
                    else
                    {
                        var deserializer = Deserializers.GetDeserializer(param.ParameterType) ?? throw new ArgumentException($"No deserializer found for parameter type {param.ParameterType} in method {method.Name}");
                        cachedDeserializers[i] = deserializer;

                        var deserializerConstant = Expression.Constant(deserializer, typeof(Deserializers.Deserializer));

                        var deserializerInvokeMethod = typeof(Deserializers.Deserializer).GetMethod("Invoke");

                        Type dataReadResultType = typeof(DataReadResult<>).MakeGenericType(param.ParameterType);

                        var deserializerResultVariable = Expression.Variable(dataReadResultType, $"deserializedResult_{param.Name}");
                        localVariables.Add(deserializerResultVariable);

                        var deserializeCall = Expression.Call(
                            deserializerConstant,
                            deserializerInvokeMethod,
                            streamParam
                        );
                        blockExpressions.Add(Expression.Assign(deserializerResultVariable, Expression.Convert(deserializeCall, dataReadResultType)));

                        var successProperty = Expression.Property(deserializerResultVariable, "Success");

                        var valueProperty = Expression.Property(deserializerResultVariable, "Value");

                        var checkFailureAndReturn = Expression.IfThen(
                            Expression.IsFalse(successProperty),
                            Expression.Return(returnLabel)
                        );
                        blockExpressions.Add(checkFailureAndReturn);

                        methodCallArgs.Add(Expression.Convert(valueProperty, param.ParameterType));
                    }
                }

                Expression instanceExpression = method.IsStatic ? null : Expression.Convert(Expression.Constant(target), method.DeclaringType);

                var methodCall = Expression.Call(instanceExpression, method, methodCallArgs);

                if (method.ReturnType != typeof(void))
                {
                    throw new ArgumentException($"Reflection handlers must have a void return type. Method {method.Name} returns {method.ReturnType.Name}.");
                }

                blockExpressions.Add(methodCall);

                blockExpressions.Add(Expression.Label(returnLabel));

                var body = Expression.Block(localVariables, blockExpressions);

                return Expression.Lambda<MessageDelegate>(
                    body,
                    connectionUIDParam,
                    messageMetadataParam,
                    streamParam
                ).Compile();
            }
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
    /// Specifies that a method should be registered as a direct message handler for network messages.
    /// </summary>
    /// <remarks>
    /// This attribute is used to mark methods that should be automatically registered as message handlers via reflection. Methods decorated with this attribute will be registered as direct delegate handlers with the message dispatcher, providing optimal performance for message processing.
    /// </remarks>
    /// <example>
    /// <code>
    /// [MessageHandler(1001)]
    /// public void HandlePlayerJoin(ulong connectionUID, MessageMetadataHandler metadata, DataStreamReader stream)
    /// {
    ///     // Process player join message
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageHandlerAttribute : Attribute
    {
        /// <summary>
        /// Gets the message key that this handler is registered for.
        /// </summary>
        public ushort MessageKey { get; private set; }

        /// <summary>
        /// Gets the message side configuration that determines which side of the network communication this handler is intended for.
        /// This property is used to filter handlers during reflection-based registration based on the dispatcher's message side configuration.
        /// </summary>
        public MessageSide MessageSide { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerAttribute"/> class with the specified message key and message side configuration.
        /// </summary>
        /// <param name="messageKey">The numeric key identifying the message type to handle.</param>
        /// <param name="messageSide">The message side configuration that determines which side of the network communication this handler is intended for.</param>
        public MessageHandlerAttribute(ushort messageKey, MessageSide messageSide)
        {
            MessageKey = messageKey;
            MessageSide = messageSide;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerAttribute"/> class with the specified enum-based message key.
        /// </summary>
        /// <param name="messageKey">The enum value identifying the message type to handle.</param>
        public MessageHandlerAttribute(Enum messageKey, MessageSide messageSide)
        {
            MessageKey = (ushort)(object)messageKey;
            MessageSide = messageSide;
        }
    }

    /// <summary>
    /// Specifies that a method should be registered as a reflection-based message handler for network messages.
    /// </summary>
    /// <remarks>
    /// This attribute is used to mark methods that should be automatically registered as message handlers via reflection. Methods decorated with this attribute will be registered as reflection-based handlers with the message dispatcher, which provides automatic parameter deserialization and flexible method signatures.
    /// <para>
    /// Reflection-based handlers can have parameters that are automatically deserialized from the message stream, making them more convenient than direct delegate handlers when working with complex message types.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [MessageReflectionHandler(1002)]
    /// public void HandlePlayerData(ulong connectionUID, MessageMetadataHandler metadata, PlayerData playerData)
    /// {
    ///     // Process player data - PlayerData is automatically deserialized
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageReflectionHandlerAttribute : Attribute
    {
        /// <summary>
        /// Gets the message key that this handler is registered for.
        /// </summary>
        public ushort MessageKey { get; private set; }

        /// <summary>
        /// Gets the message side configuration that determines which side of the network communication this handler is intended for.
        /// This property is used to filter handlers during reflection-based registration based on the dispatcher's message side configuration.
        /// </summary>
        public MessageSide MessageSide { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageReflectionHandlerAttribute"/> class with the specified message key and message side configuration.
        /// </summary>
        /// <param name="messageKey">The numeric key identifying the message type to handle.</param>
        /// <param name="messageSide">The message side configuration that determines which side of the network communication this handler is intended for. Defaults to <see cref="MessageSide.Any"/>.</param>
        public MessageReflectionHandlerAttribute(ushort messageKey, MessageSide messageSide = MessageSide.Any)
        {
            MessageKey = messageKey;
            MessageSide = messageSide;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageReflectionHandlerAttribute"/> class with the specified enum-based message key and message side configuration.
        /// </summary>
        /// <param name="messageKey">The enum value identifying the message type to handle.</param>
        /// <param name="messageSide">The message side configuration that determines which side of the network communication this handler is intended for. Defaults to <see cref="MessageSide.Any"/>.</param>
        public MessageReflectionHandlerAttribute(Enum messageKey, MessageSide messageSide = MessageSide.Any)
        {
            MessageKey = (ushort)(object)messageKey;
            MessageSide = messageSide;
        }
    }

    /// <summary>
    /// Specifies which side of the network communication a message handler is intended for.
    /// This enum is used to filter message handlers during reflection-based registration.
    /// </summary>
    /// <remarks>
    /// The <see cref="MessageSide"/> enum allows for fine-grained control over which message handlersare registered based on whether they are intended for client-side, server-side, or both sides of the network communication.
    /// </remarks>
    [Flags]
    public enum MessageSide : byte
    {
        /// <summary>
        /// No specific message side specified. Handlers with this value will not be filtered.
        /// </summary>
        None = 0,

        /// <summary>
        /// Message handler is intended for client-side processing only.
        /// </summary>
        Client = 1 << 0,

        /// <summary>
        /// Message handler is intended for server-side processing only.
        /// </summary>
        Server = 1 << 1,

        /// <summary>
        /// Message handler is intended for both client and server-side processing.
        /// This is equivalent to <see cref="Client"/> | <see cref="Server"/>.
        /// </summary>
        Common = Client | Server,

        /// <summary>
        /// Message handler should be registered regardless of message side filtering.
        /// This value matches any message side configuration.
        /// </summary>
        Any = byte.MaxValue,
    }
}
