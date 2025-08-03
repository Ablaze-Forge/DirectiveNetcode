/// <summary>
/// Represents the result of processing a network message, indicating how the message should be handled.
/// </summary>
public enum MessageResult
{
    /// <summary>
    /// Indicates that the message was processed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Indicates that the message processing determined the client should be disconnected.
    /// </summary>
    Disconnect,

    /// <summary>
    /// Indicates that the message should be kept alive (not discarded) but no further action is required.
    /// </summary>
    KeepAlive,
}
