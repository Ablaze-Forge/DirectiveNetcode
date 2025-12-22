namespace AblazeForge.DirectiveNetcode.ConnectionData
{
    public struct UshortConnectionPermissions : IConnectionPermissions<ushort>
    {
        public ushort BaseValue => m_Value;
        private ushort m_Value;

        public UshortConnectionPermissions(ushort initialValue = 0)
        {
            m_Value = initialValue;
        }
    }
}
