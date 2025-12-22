namespace AblazeForge.DirectiveNetcode.ConnectionIdProviders
{
    public interface IConnectionIdProvider<TId>
    {
        public TId GenerateNext();
    }
}
