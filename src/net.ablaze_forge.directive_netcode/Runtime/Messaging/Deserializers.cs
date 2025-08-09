using System;
using System.Collections.Generic;
using AblazeForge.DirectiveNetcode.Unity.Extensions;
using Unity.Collections;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Messaging
{
    /// <summary>
    /// Provides a centralized registry for deserializing data streams into various types.
    /// This class maintains a dictionary of deserializer functions for different types and provides methods to register and retrieve them.
    /// </summary>
    public static class Deserializers
    {
        /// <summary>
        /// The dictionary mapping types to their corresponding deserializer functions.
        /// </summary>
        private static readonly Dictionary<Type, Deserializer> m_DeserializerMap = new();

        /// <summary>
        /// Initializes the Deserializers class by registering built-in deserializer functions for common types.
        /// </summary>
        static Deserializers()
        {
            RegisterDeserializer(ReadInt);
            RegisterDeserializer(ReadByte);
            RegisterDeserializer(ReadDateTime);
            RegisterDeserializer(ReadFloat);
            RegisterDeserializer(ReadShort);
            RegisterDeserializer(ReadString);
            RegisterDeserializer(ReadUInt);
            RegisterDeserializer(ReadUShort);
            RegisterDeserializer(ReadDouble);
            RegisterDeserializer(ReadVector2);
            RegisterDeserializer(ReadVector3);
        }

        /// <summary>
        /// Registers a typed deserializer method for the specified type.
        /// </summary>
        /// <typeparam name="T">The type for which to register the deserializer.</typeparam>
        /// <param name="typedDeserializerMethod">The typed deserializer method to register.</param>
        public static void RegisterDeserializer<T>(TypedDeserializer<DataReadResult<T>> typedDeserializerMethod)
        {
            if (typedDeserializerMethod == null)
                throw new ArgumentNullException(nameof(typedDeserializerMethod));

            Deserializer wrapperDelegate = (ref DataStreamReader stream) =>
            {
                DataReadResult<T> value = typedDeserializerMethod(ref stream);
                return value;
            };

            m_DeserializerMap[typeof(T)] = wrapperDelegate;
        }

        /// <summary>
        /// Registers a deserializer function for the specified type.
        /// </summary>
        /// <param name="type">The type for which to register the deserializer.</param>
        /// <param name="deserializer">The deserializer function to register.</param>
        public static void RegisterDeserializer(Type type, Deserializer deserializer)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            m_DeserializerMap[type] = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        }

        /// <summary>
        /// Reads an integer value from the data stream.
        /// </summary>
        /// <param name="stream">The data stream reader to read from.</param>
        /// <returns>A DataReadResult containing the read integer value or failure information.</returns>
        private static DataReadResult<int> ReadInt(ref DataStreamReader stream)
        {
            if (!stream.CanRead(sizeof(int)))
            {
                return DataReadResult<int>.Failure();
            }

            return DataReadResult.Success(stream.ReadInt());
        }

        /// <summary>
        /// Reads a byte value from the data stream.
        /// </summary>
        /// <param name="stream">The data stream reader to read from.</param>
        /// <returns>A DataReadResult containing the read byte value or failure information.</returns>
        private static DataReadResult<byte> ReadByte(ref DataStreamReader stream)
        {
            if (!stream.CanRead(sizeof(byte)))
            {
                return DataReadResult<byte>.Failure();
            }

            return DataReadResult.Success(stream.ReadByte());
        }

        /// <summary>
        /// Reads a string value from the data stream.
        /// </summary>
        /// <param name="stream">The data stream reader to read from.</param>
        /// <returns>A DataReadResult containing the read string value or failure information.</returns>
        private static DataReadResult<string> ReadString(ref DataStreamReader stream)
        {
            return stream.ReadString();
        }

        /// <summary>
        /// Reads a DateTime value from the data stream.
        /// </summary>
        /// <param name="stream">The data stream reader to read from.</param>
        /// <returns>A DataReadResult containing the read DateTime value or failure information.</returns>
        private static DataReadResult<DateTime> ReadDateTime(ref DataStreamReader stream)
        {
            return stream.ReadDateTime();
        }

        /// <summary>
        /// Reads a float value from the data stream.
        /// </summary>
        /// <param name="stream">The data stream reader to read from.</param>
        /// <returns>A DataReadResult containing the read float value or failure information.</returns>
        private static DataReadResult<float> ReadFloat(ref DataStreamReader stream)
        {
            if (!stream.CanRead(sizeof(float)))
            {
                return DataReadResult<float>.Failure();
            }

            return DataReadResult.Success(stream.ReadFloat());
        }

        /// <summary>
        /// Reads a double value from the data stream.
        /// </summary>
        /// <param name="stream">The data stream reader to read from.</param>
        /// <returns>A DataReadResult containing the read double value or failure information.</returns>
        private static DataReadResult<double> ReadDouble(ref DataStreamReader stream)
        {
            if (!stream.CanRead(sizeof(double)))
            {
                return DataReadResult<double>.Failure();
            }

            return DataReadResult.Success(stream.ReadDouble());
        }

        /// <summary>
        /// Reads a Vector2 value from the data stream.
        /// </summary>
        /// <param name="stream">The data stream reader to read from.</param>
        /// <returns>A DataReadResult containing the read Vector2 value or failure information.</returns>
        private static DataReadResult<Vector2> ReadVector2(ref DataStreamReader stream)
        {
            return stream.ReadVector2();
        }

        /// <summary>
        /// Reads a Vector3 value from the data stream.
        /// </summary>
        /// <param name="stream">The data stream reader to read from.</param>
        /// <returns>A DataReadResult containing the read Vector3 value or failure information.</returns>
        private static DataReadResult<Vector3> ReadVector3(ref DataStreamReader stream)
        {
            return stream.ReadVector3();
        }

        /// <summary>
        /// Reads a short value from the data stream.
        /// </summary>
        /// <param name="stream">The data stream reader to read from.</param>
        /// <returns>A DataReadResult containing the read short value or failure information.</returns>
        private static DataReadResult<short> ReadShort(ref DataStreamReader stream)
        {
            if (!stream.CanRead(sizeof(short)))
            {
                return DataReadResult<short>.Failure();
            }

            return DataReadResult.Success(stream.ReadShort());
        }

        /// <summary>
        /// Reads a uint value from the data stream.
        /// </summary>
        /// <param name="stream">The data stream reader to read from.</param>
        /// <returns>A DataReadResult containing the read uint value or failure information.</returns>
        private static DataReadResult<uint> ReadUInt(ref DataStreamReader stream)
        {
            if (!stream.CanRead(sizeof(uint)))
            {
                return DataReadResult<uint>.Failure();
            }

            return DataReadResult.Success(stream.ReadUInt());
        }

        /// <summary>
        /// Reads a ushort value from the data stream.
        /// </summary>
        /// <param name="stream">The data stream reader to read from.</param>
        /// <returns>A DataReadResult containing the read ushort value or failure information.</returns>
        private static DataReadResult<ushort> ReadUShort(ref DataStreamReader stream)
        {
            if (!stream.CanRead(sizeof(ushort)))
            {
                return DataReadResult<ushort>.Failure();
            }

            return DataReadResult.Success(stream.ReadUShort());
        }

        /// <summary>
        /// Gets the deserializer function registered for the specified type.
        /// </summary>
        /// <param name="type">The type for which to retrieve the deserializer.</param>
        /// <returns>The deserializer function registered for the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the type parameter is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no deserializer is registered for the specified type.</exception>
        public static Deserializer GetDeserializer(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type), "Type cannot be null when requesting a deserializer.");
            }

            if (m_DeserializerMap.TryGetValue(type, out Deserializer deserializer))
            {
                return deserializer;
            }
            else
            {
                throw new InvalidOperationException($"No deserializer registered for type: {type.FullName}");
            }
        }

        /// <summary>
        /// Represents a delegate for deserializing data from a DataStreamReader into an object.
        /// </summary>
        /// <param name="stream">The data stream reader to deserialize from.</param>
        /// <returns>The deserialized object.</returns>
        public delegate object Deserializer(ref DataStreamReader stream);

        /// <summary>
        /// Represents a delegate for deserializing data from a DataStreamReader into a specific type.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize.</typeparam>
        /// <param name="stream">The data stream reader to deserialize from.</param>
        /// <returns>The deserialized object of type T.</returns>
        public delegate T TypedDeserializer<T>(ref DataStreamReader stream);
    }
}
