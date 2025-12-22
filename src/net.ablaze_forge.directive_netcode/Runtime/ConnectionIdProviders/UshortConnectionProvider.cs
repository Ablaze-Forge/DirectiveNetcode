namespace AblazeForge.DirectiveNetcode.ConnectionIdProviders
{
    public class UshortConnectionProvider : IConnectionIdProvider<ushort>
    {
        private readonly object m_Lock = new();

        private ushort m_Current = 0;

        public ushort GenerateNext()
        {
            lock (m_Lock)
            {
                return m_Current++;
            }
        }
    }
}
