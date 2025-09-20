using System;

namespace AblazeForge.DirectiveNetcode.Messaging.MessageAttributes
{
    public abstract class MessageDelegateAttributeBase : Attribute
    {
        /// <summary>
        /// Gets the message key that this handler is registered for.
        /// </summary>
        public ushort MessageKey { get; protected set; }

        /// <summary>
        /// The connection flags required for the handler to be invoked.
        /// </summary>
        public ushort RequiredConnectionFlags { get; protected set; }

        /// <summary>
        /// Gets the message side configuration that determines which side of the network communication this handler is intended for.
        /// This property is used to filter handlers during reflection-based registration based on the dispatcher's message side configuration.
        /// </summary>
        public MessageSide MessageSide { get; protected set; }

        protected MessageDelegateAttributeBase(ushort messageKey, MessageSide messageSide, ushort requiredConnectionFlags)
        {
            MessageKey = messageKey;
            MessageSide = messageSide;
            RequiredConnectionFlags = requiredConnectionFlags;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MessageAttribute : MessageDelegateAttributeBase
    {
        public MessageAttribute(ushort messageKey, MessageSide messageSide = MessageSide.Any, ushort requiredConnectionFlags = 0) : base(messageKey, messageSide, requiredConnectionFlags) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CodeGenMessageAttribute : MessageDelegateAttributeBase
    {
        public CodeGenMessageAttribute(ushort messageKey, MessageSide messageSide = MessageSide.Any, ushort requiredConnectionFlags = 0) : base(messageKey, messageSide, requiredConnectionFlags) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ControlMessageAttribute : MessageDelegateAttributeBase
    {
        public ushort StreamLength { get; protected set; }

        public ControlMessageAttribute(byte messageKey, ushort streamLength, MessageSide messageSide = MessageSide.Any, ushort requiredConnectionFlags = 0)
            : base(messageKey, messageSide, requiredConnectionFlags)
        {
            StreamLength = streamLength;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CodeGenControlMessageAttribute : MessageDelegateAttributeBase
    {
        public ushort StreamLength { get; protected set; }

        public CodeGenControlMessageAttribute(byte messageKey, ushort streamLength, MessageSide messageSide = MessageSide.Any, ushort requiredConnectionFlags = 0)
            : base(messageKey, messageSide, requiredConnectionFlags)
        {
            StreamLength = streamLength;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class EventMessageAttribute : MessageDelegateAttributeBase
    {
        public EventMessageAttribute(ushort messageKey, MessageSide messageSide = MessageSide.Any, ushort requiredConnectionFlags = 0) : base(messageKey, messageSide, requiredConnectionFlags) { }
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