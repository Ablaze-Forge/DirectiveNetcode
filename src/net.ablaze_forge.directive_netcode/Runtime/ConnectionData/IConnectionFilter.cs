using Unity.Networking.Transport;

namespace AblazeForge.DirectiveNetcode.ConnectionData
{
    public interface IConnectionFilter
    {
        bool ShouldAllowConnection(NetworkConnection connection);
    }

    public class NoOpConnectionFilter : IConnectionFilter
    {
        public bool ShouldAllowConnection(NetworkConnection connection)
        {
            return true;
        }
    }
}
