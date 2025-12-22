using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public readonly struct MessageMetadata : IMessageMetadata, IDataStreamSerializable, IDataStreamDeserializable<MessageMetadata>
    {
        public static MessageMetadata NewDefaultMessage => new(MessageType.Default, 0);

        private const byte MessageTypeReservedBitSize = 4;
        private const ushort MessageTypeMask = (1 << MessageTypeReservedBitSize) - 1;

        public MessageMetadata Deserialize(ref DataStreamReader reader)
        {
            return new MessageMetadata(reader.ReadUShort());
        }

        public MessageMetadata(MessageType messageType, ushort extraMetadata)
        {
            ushort mask = MessageTypeMask << (sizeof(ushort) * 8 - MessageTypeReservedBitSize);

            if((extraMetadata & mask) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(extraMetadata), "extraMetadata value exceeds maximum value.");
            }

            ushort value = (ushort)((extraMetadata << MessageTypeReservedBitSize) | (ushort)messageType);

            m_MetadataValue = value;
        }

        public MessageMetadata(ushort metadata)
        {
            m_MetadataValue = metadata;
        }

        public readonly bool this[byte n] => GetFlagN(n);

        public readonly MessageType MessageType => (MessageType)(m_MetadataValue & MessageTypeMask);

        private readonly ushort m_MetadataValue;

        private readonly bool GetFlagN(byte n)
        {
            if (n >= sizeof(ushort) * 8 - MessageTypeReservedBitSize)
            {
                return false;
            }

            ushort mask = (ushort)(1 << (n + MessageTypeReservedBitSize));

            return (m_MetadataValue & mask) != 0;
        }

        public bool Serialize(ref DataStreamWriter writer)
        {
            return writer.WriteUShort(m_MetadataValue);
        }
    }
}
