using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Unity.Extensions
{
    /// <summary>
    /// Provides extension methods for the Unity <see cref="DataStreamReader"/> class to facilitate reading various data types from network streams.
    /// These extensions handle proper error checking and return structured results for robust network communication.
    /// </summary>
    public static class DataStreamReaderExtensions
    {
        /// <summary>
        /// Reads a <see cref="DateTime"/> value from the data stream.
        /// </summary>
        /// <param name="reader">The data stream reader to read from.</param>
        /// <param name="dateTimeKind">The kind of DateTime to create (UTC, Local, or Unspecified). Defaults to UTC.</param>
        /// <returns>A <see cref="DataReadResult{DateTime}"/> containing the read value or failure information.</returns>
        public static DataReadResult<DateTime> ReadDateTime(this ref DataStreamReader reader, DateTimeKind dateTimeKind = DateTimeKind.Utc)
        {
            if (!reader.CanReadFixedLength(sizeof(long)))
            {
                return DataReadResult<DateTime>.Failure();
            }

            long ticks = reader.ReadLong();

            return DataReadResult.Success(new DateTime(ticks, dateTimeKind));
        }

        /// <summary>
        /// Reads a string value from the data stream.
        /// </summary>
        /// <param name="reader">The data stream reader to read from.</param>
        /// <returns>A <see cref="DataReadResult{string}"/> containing the read value or failure information.</returns>
        public static DataReadResult<string> ReadString(this ref DataStreamReader reader)
        {
            if (!reader.CanReadFixedLength(sizeof(int)))
            {
                return DataReadResult<string>.Failure();
            }

            int byteLength = reader.ReadInt();

            if (byteLength is 0)
            {
                return DataReadResult.Success(string.Empty);
            }

            if (byteLength is -1)
            {
                return DataReadResult.Success<string>(null);
            }

            if (byteLength < 0)
            {
                return DataReadResult<string>.Failure();
            }

            if (!reader.CanReadFixedLength(byteLength))
            {
                return DataReadResult<string>.Failure();
            }

            Span<byte> stringBytes = new byte[byteLength];

            reader.ReadBytes(stringBytes);

            return DataReadResult.Success(System.Text.Encoding.UTF8.GetString(stringBytes));
        }

        /// <summary>
        /// Reads a <see cref="Vector2"/> value from the data stream.
        /// </summary>
        /// <param name="reader">The data stream reader to read from.</param>
        /// <returns>A <see cref="DataReadResult{Vector2}"/> containing the read value or failure information.</returns>
        public static DataReadResult<Vector2> ReadVector2(this ref DataStreamReader reader)
        {
            if (!reader.CanReadFixedLength(sizeof(float) * 2))
            {
                return DataReadResult<Vector2>.Failure();
            }

            float x = reader.ReadFloat();
            float y = reader.ReadFloat();

            return DataReadResult.Success(new Vector2(x, y));
        }

        /// <summary>
        /// Reads a <see cref="Vector3"/> value from the data stream.
        /// </summary>
        /// <param name="reader">The data stream reader to read from.</param>
        /// <returns>A <see cref="DataReadResult{Vector3}"/> containing the read value or failure information.</returns>
        public static DataReadResult<Vector3> ReadVector3(this ref DataStreamReader reader)
        {
            if (!reader.CanReadFixedLength(sizeof(float) * 3))
            {
                return DataReadResult<Vector3>.Failure();
            }

            float x = reader.ReadFloat();
            float y = reader.ReadFloat();
            float z = reader.ReadFloat();

            return DataReadResult.Success(new Vector3(x, y, z));
        }

        /// <summary>
        /// Ensures that the data stream reader can read the specified number of bytes, throwing an exception if not.
        /// </summary>
        /// <param name="reader">The data stream reader to check.</param>
        /// <param name="requiredBytes">The number of bytes required to read.</param>
        /// <param name="type">The type of data being read, used in error messages.</param>
        /// <exception cref="DataStreamReaderLengthReadException">Thrown when there are not enough bytes in the stream or when integer overflow would occur.</exception>
        public static void EnsureCanRead(this ref DataStreamReader reader, int requiredBytes, Type type)
        {
            int bytesRead = reader.GetBytesRead();

            if (requiredBytes < 0)
            {
                throw new DataStreamReaderLengthReadException($"Unable to read {type.Name}. Invalid negative required bytes: {requiredBytes}.");
            }

            if (bytesRead > int.MaxValue - requiredBytes)
            {
                throw new DataStreamReaderLengthReadException($"Unable to read {type.Name}. Data size request ({requiredBytes}) combined with current read position ({bytesRead}) would exceed maximum stream capacity or integer limits.");
            }

            if (reader.Length < bytesRead + requiredBytes)
            {
                throw new DataStreamReaderLengthReadException($"Unable to read {type.Name}. Not enough bytes in stream, required at least {requiredBytes}.");
            }
        }

        /// <summary>
        /// Ensures that the data stream reader can read the specified number of bytes with fixed length, throwing an exception if not.
        /// </summary>
        /// <param name="reader">The data stream reader to check.</param>
        /// <param name="requiredBytes">The number of bytes required to read.</param>
        /// <param name="type">The type of data being read, used in error messages.</param>
        /// <exception cref="DataStreamReaderLengthReadException">Thrown when there are not enough bytes in the stream or when integer overflow would occur.</exception>
        public static void EnsureCanReadFixedLength(this ref DataStreamReader reader, int requiredBytes, Type type)
        {
            int bytesRead = reader.GetBytesRead();

            if (bytesRead > int.MaxValue - requiredBytes)
            {
                throw new DataStreamReaderLengthReadException($"Unable to read {type.Name}. Data size request ({requiredBytes}) combined with current read position ({bytesRead}) would exceed maximum stream capacity or integer limits.");
            }

            if (reader.Length < bytesRead + requiredBytes)
            {
                throw new DataStreamReaderLengthReadException($"Unable to read {type.Name}. Not enough bytes in stream, required at least {requiredBytes}.");
            }
        }

        /// <summary>
        /// Checks if the data stream reader can read the specified number of bytes.
        /// </summary>
        /// <param name="reader">The data stream reader to check.</param>
        /// <param name="requiredBytes">The number of bytes required to read.</param>
        /// <returns><c>true</c> if the reader can read the specified number of bytes; otherwise, <c>false</c>.</returns>
        public static bool CanRead(this ref DataStreamReader reader, int requiredBytes)
        {
            int bytesRead = reader.GetBytesRead();

            if (requiredBytes < 0) return false;

            if (bytesRead > int.MaxValue - requiredBytes) return false;

            return reader.Length >= bytesRead + requiredBytes;
        }

        /// <summary>
        /// Checks if the data stream reader can read the specified number of bytes with fixed length.
        /// </summary>
        /// <param name="reader">The data stream reader to check.</param>
        /// <param name="requiredBytes">The number of bytes required to read.</param>
        /// <returns><c>true</c> if the reader can read the specified number of bytes; otherwise, <c>false</c>.</returns>
        public static bool CanReadFixedLength(this ref DataStreamReader reader, int requiredBytes)
        {
            int bytesRead = reader.GetBytesRead();

            if (bytesRead > int.MaxValue - requiredBytes) return false;

            return reader.Length >= bytesRead + requiredBytes;
        }
    }

    /// <summary>
    /// Represents the result of attempting to read data from a stream, indicating success or failure and containing the read value if successful.
    /// </summary>
    /// <typeparam name="T">The type of value being read from the stream.</typeparam>
    public class DataReadResult<T>
    {
        /// <summary>
        /// Gets a value indicating whether the read operation was successful.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Gets the value read from the stream if the operation was successful; otherwise, the default value for the type.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataReadResult{T}"/> class with the specified value and success status.
        /// </summary>
        /// <param name="value">The value read from the stream.</param>
        /// <param name="result">The success status of the read operation.</param>
        internal DataReadResult(T value, bool result)
        {
            IsSuccess = result;
            Value = value;
        }

        /// <summary>
        /// Creates a new <see cref="DataReadResult{T}"/> instance indicating a failed read operation.
        /// </summary>
        /// <returns>A new <see cref="DataReadResult{T}"/> instance with success set to <c>false</c> and the default value for type T.</returns>
        public static DataReadResult<T> Failure()
        {
            return new DataReadResult<T>(default, false);
        }
    }

    /// <summary>
    /// Provides factory methods for creating successful <see cref="DataReadResult{T}"/> instances.
    /// </summary>
    public class DataReadResult
    {
        /// <summary>
        /// Creates a new <see cref="DataReadResult{T}"/> instance indicating a successful read operation with the specified value.
        /// </summary>
        /// <typeparam name="T">The type of value being read from the stream.</typeparam>
        /// <param name="value">The value read from the stream.</param>
        /// <returns>A new <see cref="DataReadResult{T}"/> instance with success set to <c>true</c> and the specified value.</returns>
        public static DataReadResult<T> Success<T>(T value)
        {
            return new DataReadResult<T>(value, true);
        }
    }

    /// <summary>
    /// Represents an exception that occurs when attempting to read data from a stream with insufficient bytes or invalid parameters.
    /// </summary>
    public class DataStreamReaderLengthReadException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataStreamReaderLengthReadException"/> class with the specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DataStreamReaderLengthReadException(string message) : base(message) { }
    }
}
