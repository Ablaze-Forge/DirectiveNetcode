namespace AblazeForge.DirectiveNetcode.Engines
{
    /// <summary>
    /// Defines the indices for the different network pipelines used for communication.
    /// This enum is shared between the client and server to ensure consistent identification of pipelines.
    /// </summary>
    public enum NetworkPipelineIndex
    {
        /// <summary>
        /// Corresponds to an unreliable, unordered pipeline (e.g., standard UDP).
        /// </summary>
        Unreliable,
        /// <summary>
        /// Corresponds to a reliable, ordered pipeline (e.g., UDP with sequencing and acks).
        /// </summary>
        Reliable,
        /// <summary>
        /// Corresponds to an unreliable, but ordered pipeline (e.g., sequenced UDP).
        /// </summary>
        UnreliableSequenced,
        /// <summary>
        /// Corresponds to a pipeline that supports message fragmentation for sending large packets.
        /// </summary>
        Fragmented,
    }
}
