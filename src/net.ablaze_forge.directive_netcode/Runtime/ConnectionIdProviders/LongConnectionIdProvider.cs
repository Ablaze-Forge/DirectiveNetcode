using System.Threading;

namespace AblazeForge.DirectiveNetcode.ConnectionIdProviders
{
    public class LongConnectionIdProvider : IConnectionIdProvider<long>
    {
        private long currentId;

        public LongConnectionIdProvider()
        {
            currentId = 0;
        }

        public long GenerateNext()
        {
            return Interlocked.Increment(ref currentId);
        }
    }
}
