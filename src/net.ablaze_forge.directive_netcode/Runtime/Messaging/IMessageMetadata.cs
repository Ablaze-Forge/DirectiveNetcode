namespace AblazeForge.DirectiveNetcode.Messaging
{
    public interface IMessageMetadata
    {
        public MessageType MessageType { get; }

        public bool this[byte n] { get; }
    }
}
