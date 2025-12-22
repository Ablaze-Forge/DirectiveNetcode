using System.Threading;

namespace AblazeForge.DirectiveNetcode.ConnectionIdProviders
{
    public class IntConnectionIdProvider : IConnectionIdProvider<int>
    {
        private int currentId;

        public IntConnectionIdProvider()
        {
            currentId = 0;
        }

        public int GenerateNext()
        {
            return Interlocked.Increment(ref currentId);
        }
    }
}