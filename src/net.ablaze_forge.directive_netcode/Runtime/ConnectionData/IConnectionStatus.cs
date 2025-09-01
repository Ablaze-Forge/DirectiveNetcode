using System;

namespace AblazeForge.DirectiveNetcode.ConnectionData
{
    public interface IConnectionStatus
    {
        public const byte FlagsSize = sizeof(ushort);
        public ushort CurrentFlags { get; }

        public bool MeetsCriteria(ushort requiredFlags)
        {
            return (CurrentFlags & requiredFlags) == requiredFlags;
        }

        public bool HasStatus(byte bitIndex);
        public void SetStatus(byte bitIndex);
        public void UnsetStatus(byte bitIndex);
    }

    public class ConnectionStatus : IConnectionStatus
    {
        public ConnectionStatus(ushort initialState)
        {
            m_CurrentFlags = initialState;
        }

        public ushort CurrentFlags => m_CurrentFlags;
        private ushort m_CurrentFlags;

        public bool HasStatus(byte bitIndex)
        {
            if (bitIndex < 0 || bitIndex > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index must be between 0 and 15.");
            }

            ushort mask = (ushort)(1 << bitIndex);
            return (m_CurrentFlags & mask) != 0;
        }

        public void SetStatus(byte bitIndex)
        {
            if (bitIndex < 0 || bitIndex > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index must be between 0 and 15.");
            }

            m_CurrentFlags |= (ushort)(1 << bitIndex);
        }

        public void UnsetStatus(byte bitIndex)
        {
            if (bitIndex < 0 || bitIndex > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index must be between 0 and 15.");
            }

            m_CurrentFlags &= (ushort)~(1 << bitIndex);
        }
    }
}
