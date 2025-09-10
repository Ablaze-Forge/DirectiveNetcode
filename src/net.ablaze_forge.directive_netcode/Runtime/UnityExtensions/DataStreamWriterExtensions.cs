using System;
using Unity.Collections;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Unity.Extensions
{
    /// <summary>
    /// Provides extension methods for the Unity <see cref="DataStreamWriter"/> class to facilitate writing various data types to network streams.
    /// These extensions handle proper error checking and return boolean values indicating success or failure for robust network communication.
    /// </summary>
    public static class DataStreamWriterExtensions
    {
        /// <summary>
        /// Writes a <see cref="DateTime"/> value to the data stream.
        /// </summary>
        /// <param name="writer">The data stream writer to write to.</param>
        /// <param name="dateTime">The DateTime value to write.</param>
        /// <returns><c>true</c> if the value was successfully written; otherwise, <c>false</c>.</returns>
        public static bool WriteDateTime(this ref DataStreamWriter writer, DateTime dateTime)
        {
            if (!writer.CanWrite(sizeof(long)))
            {
                return false;
            }

            return writer.WriteLong(dateTime.Ticks);
        }

        /// <summary>
        /// Writes a string value to the data stream.
        /// </summary>
        /// <param name="writer">The data stream writer to write to.</param>
        /// <param name="value">The string value to write.</param>
        /// <returns><c>true</c> if the value was successfully written; otherwise, <c>false</c>.</returns>
        public static bool WriteString(this ref DataStreamWriter writer, string value)
        {
            // Write -1 for null values
            if (value is null)
            {
                if (!writer.CanWrite(sizeof(int)))
                {
                    return false;
                }
                return writer.WriteInt(-1);
            }

            // Write 0 for empty strings
            if (value.Length is 0)
            {
                if (!writer.CanWrite(sizeof(int)))
                {
                    return false;
                }

                return writer.WriteInt(0);
            }

            byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes(value);
            int byteLength = stringBytes.Length;

            // Check if we can write the length + the bytes
            if (!writer.CanWrite(sizeof(int) + byteLength))
            {
                return false;
            }

            // Write the length first
            if (!writer.WriteInt(byteLength))
            {
                return false;
            }

            // Write the actual string bytes
            return writer.WriteBytes(stringBytes);
        }

        /// <summary>
        /// Writes a <see cref="Vector2"/> value to the data stream.
        /// </summary>
        /// <param name="writer">The data stream writer to write to.</param>
        /// <param name="value">The Vector2 value to write.</param>
        /// <returns><c>true</c> if the value was successfully written; otherwise, <c>false</c>.</returns>
        public static bool WriteVector2(this ref DataStreamWriter writer, Vector2 value)
        {
            if (!writer.CanWrite(sizeof(float) * 2))
            {
                return false;
            }

            return writer.WriteFloat(value.x) && writer.WriteFloat(value.y);
        }

        /// <summary>
        /// Writes a <see cref="Vector3"/> value to the data stream.
        /// </summary>
        /// <param name="writer">The data stream writer to write to.</param>
        /// <param name="value">The Vector3 value to write.</param>
        /// <returns><c>true</c> if the value was successfully written; otherwise, <c>false</c>.</returns>
        public static bool WriteVector3(this ref DataStreamWriter writer, Vector3 value)
        {
            if (!writer.CanWrite(sizeof(float) * 3))
            {
                return false;
            }

            return writer.WriteFloat(value.x) && writer.WriteFloat(value.y) && writer.WriteFloat(value.z);
        }

        /// <summary>
        /// Ensures that the data stream writer can write the specified number of bytes, throwing an exception if not.
        /// </summary>
        /// <param name="writer">The data stream writer to check.</param>
        /// <param name="requiredBytes">The number of bytes required to write.</param>
        /// <param name="type">The type of data being written, used in error messages.</param>
        /// <exception cref="DataStreamWriterOverflowException">Thrown when there are not enough bytes in the stream or when integer overflow would occur.</exception>
        public static void EnsureCanWrite(this ref DataStreamWriter writer, int requiredBytes, Type type)
        {
            int bytesWriten = writer.Length;

            if (requiredBytes <= 0)
            {
                throw new DataStreamWriterOverflowException($"Unable to write {type.Name}. Invalid negative or zero required bytes: {requiredBytes}.");
            }

            if (requiredBytes > int.MaxValue - bytesWriten)
            {
                throw new DataStreamWriterOverflowException($"Unable to write {type.Name}. Data size request ({requiredBytes}) combined with current read position ({bytesWriten}) would exceed integer limits.");
            }

            if (writer.Capacity < bytesWriten + requiredBytes)
            {
                throw new DataStreamWriterOverflowException($"Unable to write {type.Name}. Writing it would exceed stream capacity of {writer.Capacity}, required at least {requiredBytes} free bytes.");
            }
        }

        /// <summary>
        /// Ensures that the data stream writer can write the specified number of bytes with fixed length, throwing an exception if not.
        /// </summary>
        /// <param name="writer">The data stream writer to check.</param>
        /// <param name="requiredBytes">The number of bytes required to write.</param>
        /// <param name="type">The type of data being written, used in error messages.</param>
        /// <exception cref="DataStreamWriterOverflowException">Thrown when there are not enough bytes in the stream or when integer overflow would occur.</exception>
        public static void EnsureCanWriteFixedLength(this ref DataStreamWriter writer, int requiredBytes, Type type)
        {
            int bytesWriten = writer.Length;

            if (requiredBytes < 0)
            {
                throw new DataStreamWriterOverflowException($"Unable to write {type.Name}. Invalid negative required bytes: {requiredBytes}.");
            }

            if (bytesWriten > int.MaxValue - requiredBytes)
            {
                throw new DataStreamWriterOverflowException($"Unable to write {type.Name}. Data size request ({requiredBytes}) combined with current read position ({bytesWriten}) would exceed maximum stream capacity or integer limits.");
            }

            if (writer.Capacity < bytesWriten + requiredBytes)
            {
                throw new DataStreamWriterOverflowException($"Unable to write {type.Name}. Not enough bytes in stream, required at least {requiredBytes}.");
            }
        }

        /// <summary>
        /// Checks if the data stream writer can write the specified number of bytes.
        /// </summary>
        /// <param name="writer">The data stream writer to check.</param>
        /// <param name="requiredBytes">The number of bytes required to write.</param>
        /// <returns><c>true</c> if the writer can write the specified number of bytes; otherwise, <c>false</c>.</returns>
        public static bool CanWrite(this ref DataStreamWriter writer, int requiredBytes)
        {
            int bytesWriten = writer.Length;

            if (requiredBytes <= 0)
            {
                return false;
            }

            if (requiredBytes > int.MaxValue - bytesWriten)
            {
                return false;
            }

            if (writer.Capacity < bytesWriten + requiredBytes)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the data stream writer can write the specified number of bytes with fixed length.
        /// </summary>
        /// <param name="writer">The data stream writer to check.</param>
        /// <param name="requiredBytes">The number of bytes required to write.</param>
        /// <returns><c>true</c> if the writer can write the specified number of bytes; otherwise, <c>false</c>.</returns>
        public static bool CanWriteFixedLength(this ref DataStreamWriter writer, int requiredBytes)
        {
            int bytesWriten = writer.Length;

            if (requiredBytes < 0) return false;

            if (bytesWriten > int.MaxValue - requiredBytes) return false;

            return writer.Capacity >= bytesWriten + requiredBytes;
        }
    }

    /// <summary>
    /// Represents an exception that occurs when attempting to write data to a stream with insufficient capacity or invalid parameters.
    /// </summary>
    public class DataStreamWriterOverflowException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataStreamWriterOverflowException"/> class with the specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DataStreamWriterOverflowException(string message) : base(message) { }
    }
}
