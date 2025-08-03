/// <summary>
/// Handles metadata for network messages, encoding information about message types and characteristics in a single byte.
/// This class interprets specific bit patterns in the metadata byte to determine message properties.
/// </summary>
public class MessageMetadataHandler
{
    // Booleans defining what type of message this is, bits 6 and 7 are locked for the comparison, allowing for up to 4 types
    private const byte m_MessageTypeBitMask = 0b11000000;

    /// <summary>
    /// Gets a value indicating whether this message is of the default type.
    /// This is determined by checking if bits 6 and 7 of the metadata byte are both 0 (0b00000000).
    /// </summary>
    public bool IsDefaultType => (m_Data & m_MessageTypeBitMask) == 0b00000000;

    /// <summary>
    /// Gets a value indicating whether this message is of the variable tracking type.
    /// This is determined by checking if bits 6 and 7 of the metadata byte are 01 (0b01000000).
    /// </summary>
    public bool IsVarTracking => (m_Data & m_MessageTypeBitMask) == 0b01000000;

    private readonly byte m_Data;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageMetadataHandler"/> class with the specified metadata byte.
    /// </summary>
    /// <param name="data">The byte containing message metadata, with bits 6-7 reserved for message type identification.</param>
    public MessageMetadataHandler(byte data)
    {
        m_Data = data;
    }
}
