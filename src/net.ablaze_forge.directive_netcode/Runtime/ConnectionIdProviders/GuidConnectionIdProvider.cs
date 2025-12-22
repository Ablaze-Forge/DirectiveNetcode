using System;

namespace AblazeForge.DirectiveNetcode.ConnectionIdProviders
{
    public class GuidConnectionIdProvider : IConnectionIdProvider<Guid>
    {
        public Guid GenerateNext()
        {
            return Guid.NewGuid();
        }
    }
}
