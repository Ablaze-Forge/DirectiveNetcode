namespace AblazeForge.DirectiveNetcode.ConnectionData
{
    public class ConnectionInformation
    {
        public readonly ulong ConnectionUid;
        public readonly IConnectionStatus Status;

        public ConnectionInformation(ulong connectionUid, ushort status = 0)
        {
            ConnectionUid = connectionUid;
            Status = new ConnectionStatus(status);
        }
    }
}
