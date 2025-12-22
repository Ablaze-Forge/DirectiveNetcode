namespace AblazeForge.DirectiveNetcode.Messaging
{
    public enum MessageSendType : byte
    {
        Unreliable,
        Reliable,
        Unreliable_Ordered,
        Fragmented,
    }
}
