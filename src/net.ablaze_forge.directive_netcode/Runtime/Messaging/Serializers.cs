using System;
using Unity.Collections;
using AblazeForge.DirectiveNetcode.Unity.Extensions;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public static class Serializers
    {
        public delegate bool TypedSerializerDelegate<T>(ref DataStreamWriter stream, T value);

        public static void RegisterCommonTypes()
        {
            Register((ref DataStreamWriter writer, byte value) => writer.WriteByte(value));
            Register((ref DataStreamWriter writer, ushort value) => writer.WriteUShort(value));
            Register((ref DataStreamWriter writer, uint value) => writer.WriteUInt(value));
            Register((ref DataStreamWriter writer, ulong value) => writer.WriteULong(value));
            Register((ref DataStreamWriter writer, float value) => writer.WriteFloat(value));
            Register((ref DataStreamWriter writer, double value) => writer.WriteDouble(value));
            Register((ref DataStreamWriter writer, short value) => writer.WriteShort(value));
            Register((ref DataStreamWriter writer, int value) => writer.WriteInt(value));
            Register((ref DataStreamWriter writer, long value) => writer.WriteLong(value));
            Register((ref DataStreamWriter writer, string value) => writer.WriteString(value));
            Register((ref DataStreamWriter writer, DateTime value) => writer.WriteDateTime(value));
        }

        /// <summary>
        /// Registers a serializer for a specific type.
        /// </summary>
        public static void Register<T>(TypedSerializerDelegate<T> serializer)
        {
            SerializerCache<T>.Serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
        }

        public static bool TryWrite<T>(this ref DataStreamWriter stream, T value)
        {
            try
            {
                return SerializerCache<T>.Serializer.Invoke(ref stream, value);
            }
            catch
            {
                return false;
            }
        }

        public static bool Write<T>(this ref DataStreamWriter stream, T value)
        {
            return SerializerCache<T>.Serializer.Invoke(ref stream, value);
        }

        /// <summary>
        /// Gets the serializer for a specific type.
        /// </summary>
        public static TypedSerializerDelegate<T> GetSerializer<T>()
        {
            if (SerializerCache<T>.Serializer != null)
            {
                return SerializerCache<T>.Serializer;
            }

            throw new InvalidOperationException($"No serializer registered for type: {typeof(T).FullName}");
        }

        private static class SerializerCache<T>
        {
            internal static TypedSerializerDelegate<T> Serializer = null;
        }
    }
}