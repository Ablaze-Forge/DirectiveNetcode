namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Handles metadata for network messages, encoding information about message types and characteristics in a single byte.
    /// This class interprets specific bit patterns in the metadata byte to determine message properties.
    /// </summary>
    public class MessageMetadataHandler
    {
        /// <summary>
        /// Gets a new default message metadata handler instance.
        /// This instance represents a message with the default type (bits 6-7 set to 00).
        /// </summary>
        public static MessageMetadataHandler Default => new(0b00_000000);

        /// <summary>
        /// Gets a new variable tracking message metadata handler instance.
        /// This instance represents a message with the variable tracking type (bits 6-7 set to 01).
        /// </summary>
        public static MessageMetadataHandler VarTracking => new(0b01_000000);

        /// <summary>
        /// Gets a new event message metadata handler instance.
        /// This instance represents a message with the variable tracking type (bits 6-7 set to 10).
        /// </summary>
        public static MessageMetadataHandler EventMessage => new(0b10_000000);

        /// <summary>
        /// Gets a new control message metadata handler instance.
        /// This instance represents a message with the variable tracking type (bits 6-7 set to 11).
        /// </summary>
        public static MessageMetadataHandler ControlMessage => new(0b11_000000);

        // Booleans defining what type of message this is, bits 6 and 7 are locked for the comparison, allowing for up to 4 types
        private const byte m_MessageTypeBitMask = 0b11_000000;

        /// <summary>
        /// Gets a value indicating whether this message is of the default type.
        /// This is determined by checking if bits 6 and 7 of the metadata byte are both 0 (0b00000000).
        /// </summary>
        public bool IsDefaultType => (Data & m_MessageTypeBitMask) == 0b00_000000;

        /// <summary>
        /// Gets a value indicating whether this message is of the variable tracking type.
        /// This is determined by checking if bits 6 and 7 of the metadata byte are 01 (0b01000000).
        /// </summary>
        public bool IsVarTracking => (Data & m_MessageTypeBitMask) == 0b01_000000;

        /// <summary>
        /// Gets a value indicating whether this message is of the event type.
        /// This is determined by checking if bits 6 and 7 of the metadata byte are 10 (0b10000000).
        /// </summary>
        public bool IsEvent => (Data & m_MessageTypeBitMask) == 0b10_000000;

        /// <summary>
        /// Gets a value indicating whether this message is of the control type.
        /// This is determined by checking if bits 6 and 7 of the metadata byte are 11 (0b11000000).
        /// </summary>
        public bool IsControl => (Data & m_MessageTypeBitMask) == 0b11_000000;

        /// <summary>
        /// Gets the message type as an enum, which can be used in switch statements.
        /// This property isolates bits 6 and 7 of the metadata byte to determine the type.
        /// </summary>
        public MessageType Type => (MessageType)(Data & m_MessageTypeBitMask);

        public readonly byte Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageMetadataHandler"/> class with the specified metadata byte.
        /// </summary>
        /// <param name="data">The byte containing message metadata, with bits 6-7 reserved for message type identification.</param>
        public MessageMetadataHandler(byte data)
        {
            Data = data;
        }
    }

    public enum MessageType
    {
        Default = 0b00_000000,
        VarTracking = 0b01_000000,
        Event = 0b10_000000,
        Control = 0b11_000000
    }
}
