using System;
using Unity.Collections;
using AblazeForge.DirectiveNetcode.Unity.Extensions;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    public static class Deserializers
    {
        public delegate DataReadResult<T> TypedDeserializerDelegate<T>(ref DataStreamReader stream);

        public static void RegisterCommonTypes()
        {
            Register((ref DataStreamReader reader) => WrapReadMethod(reader.ReadByte));
            Register((ref DataStreamReader reader) => WrapReadMethod(reader.ReadDouble));
            Register((ref DataStreamReader reader) => WrapReadMethod(reader.ReadFloat));
            Register((ref DataStreamReader reader) => WrapReadMethod(reader.ReadInt));
            Register((ref DataStreamReader reader) => WrapReadMethod(reader.ReadLong));
            Register((ref DataStreamReader reader) => WrapReadMethod(reader.ReadShort));
            Register((ref DataStreamReader reader) => WrapReadMethod(reader.ReadUInt));
            Register((ref DataStreamReader reader) => WrapReadMethod(reader.ReadUShort));
            Register((ref DataStreamReader reader) => WrapReadMethod(reader.ReadULong));
            Register((ref DataStreamReader reader) => reader.ReadDateTime());
            Register((ref DataStreamReader reader) => reader.ReadString());
            Register((ref DataStreamReader reader) => reader.ReadVector2());
            Register((ref DataStreamReader reader) => reader.ReadVector3());
        }

        /// <summary>
        /// Registers a deserializer for a specific type.
        /// </summary>
        public static void Register<T>(TypedDeserializerDelegate<T> deserializer)
        {
            DeserializerCache<T>.Deserializer = deserializer ??
                throw new ArgumentNullException(nameof(deserializer));
        }

        /// <summary>
        /// Gets the deserializer for a specific type.
        /// </summary>
        public static TypedDeserializerDelegate<T> GetDeserializer<T>()
        {
            if (DeserializerCache<T>.Deserializer != null)
            {
                return DeserializerCache<T>.Deserializer;
            }

            throw new InvalidOperationException($"No deserializer registered for type: {typeof(T).FullName}");
        }

        public static DataReadResult<T> ReadWithDeserializer<T>(this DataStreamReader reader)
        {
            return GetDeserializer<T>().Invoke(ref reader);
        }

        public static class DeserializerCache<T>
        {
            internal static TypedDeserializerDelegate<T> Deserializer = null;
        }

        private static DataReadResult<T> WrapReadMethod<T>(Func<T> readMethod)
        {
            try
            {
                return DataReadResult.Success(readMethod.Invoke());
            }
            catch
            {
                return DataReadResult<T>.Failure();
            }
        }
    }
}