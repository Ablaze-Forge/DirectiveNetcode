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
    public class ServerMessageDispatcher
    {
        /// <summary>
        /// The logger instance used for logging messages and errors from the dispatcher.
        /// </summary>
        private readonly ILogger m_Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerMessageDispatcher"/> class with the specified logger.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging messages and errors.</param>
        public ServerMessageDispatcher(ILogger logger)
        {
            m_Logger = logger;
        }

        /// <summary>
        /// The dictionary mapping message keys to their registered direct delegate handlers.
        /// </summary>
        private readonly Dictionary<ushort, MessageHandler> m_Handlers = new();

        /// <summary>
        /// The dictionary mapping message keys to their registered reflection-based handlers.
        /// </summary>
        private readonly Dictionary<ushort, ReflectionHandler> m_ReflectionHandlers = new();

        /// <summary>
        /// Registers a direct delegate handler for messages with the specified key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of message to handle.</param>
        /// <param name="handler">The delegate handler to register.</param>
        public void RegisterHandler(ushort messageKey, MessageHandler handler)
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
            if (m_ReflectionHandlers.ContainsKey(messageKey))
            {
                m_Logger.LogError(GetType().Name, $"Reflection handler already registered for message key {messageKey}.");
                return;
            }
            try
            {
                var reflectionHandler = new ReflectionHandler(target, method);
                m_ReflectionHandlers[messageKey] = reflectionHandler;
            }
            catch (ArgumentException ex)
            {
                m_Logger.LogError(GetType().Name, $"Failed to register reflection handler for message key {messageKey}: {ex.Message}");
            }
        }

        /// <summary>
        /// Unregisters a direct delegate handler for messages with the specified key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of message to unregister.</param>
        /// <param name="handler">The delegate handler to unregister.</param>
        public void UnregisterHandler(ushort messageKey, MessageHandler handler)
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
        /// Removes all handlers (both direct and reflection-based) for the specified message key.
        /// </summary>
        /// <param name="messageKey">The key identifying the type of message to remove.</param>
        public void RemoveMessage(ushort messageKey)
        {
            m_Handlers.Remove(messageKey);
            m_ReflectionHandlers.Remove(messageKey);
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
            else if (m_ReflectionHandlers.TryGetValue(messageKey, out var reflectionHandler))
            {
                try
                {
                    reflectionHandler.Invoke(connectionUID, messageMetadata, stream);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(GetType().Name, $"Exception invoking reflection handler for message key {messageKey}: {ex}");
                }

                return;
            }
            else
            {
                m_Logger.LogError(GetType().Name, $"Invalid message key {messageKey} to dispatch.");
            }
        }

        /// <summary>
        /// Handles reflection-based message processing by dynamically invoking methods with deserialized parameters.
        /// This class uses compiled expressions for efficient method invocation and parameter deserialization.
        /// </summary>
        private class ReflectionHandler
        {
            /// <summary>
            /// The parameter information for the subscriber method.
            /// </summary>
            private readonly ParameterInfo[] subscriberMethodParameters;

            /// <summary>
            /// The names of the parameters for the subscriber method.
            /// </summary>
            private readonly string[] parameterNames;

            /// <summary>
            /// The compiled invoker delegate for efficient method invocation.
            /// </summary>
            private readonly CompiledInvokerDelegate compiledInvoker;

            /// <summary>
            /// The cached deserializers for each parameter of the subscriber method.
            /// </summary>
            private readonly Deserializers.Deserializer[] cachedDeserializers;

            /// <summary>
            /// The name of the connection UID parameter.
            /// </summary>
            private const string connectionUIDParameterName = "connectionUID";

            /// <summary>
            /// The name of the message metadata parameter.
            /// </summary>
            private const string messageMetadataParameterName = "messageMetadata";

            /// <summary>
            /// Initializes a new instance of the <see cref="ReflectionHandler"/> class with the specified target object and method information.
            /// </summary>
            /// <param name="target">The target object instance for the method, or null for static methods.</param>
            /// <param name="method">The method information for the handler method.</param>
            /// <exception cref="ArgumentException">Thrown when target is null for non-static methods or when no deserializer is found for a parameter type.</exception>
            public ReflectionHandler(object target, MethodInfo method)
            {
                if (!method.IsStatic && target == null)
                {
                    throw new ArgumentException("Target cannot be null for non-static methods.", nameof(target));
                }
                if (method.IsStatic && target != null)
                {
                    target = null;
                }

                subscriberMethodParameters = method.GetParameters();
                parameterNames = new string[subscriberMethodParameters.Length];
                cachedDeserializers = new Deserializers.Deserializer[subscriberMethodParameters.Length];

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
                        var deserializer = Deserializers.GetDeserializer(param.ParameterType);

                        if (deserializer == null)
                        {
                            throw new ArgumentException($"No deserializer found for parameter type {param.ParameterType} in method {method.Name}");
                        }
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

                compiledInvoker = Expression.Lambda<CompiledInvokerDelegate>(
                    body,
                    connectionUIDParam,
                    messageMetadataParam,
                    streamParam
                ).Compile();
            }

            /// <summary>
            /// Invokes the handler method with the specified parameters.
            /// </summary>
            /// <param name="connectionUID">The unique identifier of the connection that sent the message.</param>
            /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
            /// <param name="stream">The data stream reader containing the message data.</param>
            public void Invoke(ulong connectionUID, MessageMetadataHandler messageMetadata, DataStreamReader stream)
            {
                compiledInvoker(connectionUID, messageMetadata, ref stream);
            }
        }

        /// <summary>
        /// Represents a delegate for compiled invoker methods that process messages with connection information, metadata, and data stream.
        /// </summary>
        /// <param name="connectionUID">The unique identifier of the connection that sent the message.</param>
        /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
        /// <param name="stream">The data stream reader containing the message data.</param>
        public delegate void CompiledInvokerDelegate(ulong connectionUID, MessageMetadataHandler messageMetadata, ref DataStreamReader stream);
    }

    /// <summary>
    /// Represents a delegate for message handlers that process messages with connection information, metadata, and data stream.
    /// </summary>
    /// <param name="connectionUID">The unique identifier of the connection that sent the message.</param>
    /// <param name="messageMetadata">The metadata handler containing information about the message type and characteristics.</param>
    /// <param name="stream">The data stream reader containing the message data.</param>
    public delegate void MessageHandler(ulong connectionUID, MessageMetadataHandler messageMetadata, DataStreamReader stream);
}
