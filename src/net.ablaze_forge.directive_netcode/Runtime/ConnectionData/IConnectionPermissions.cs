namespace AblazeForge.DirectiveNetcode.ConnectionData
{
    public interface IConnectionPermissions<T> where T : unmanaged
    {
        public T BaseValue { get; }
    }
}
