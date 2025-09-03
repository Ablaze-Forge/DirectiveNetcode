using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AblazeForge.DirectiveNetcode.Unity.Extensions.Unsafe
{
    public static class DataStreamWriterUnsafeExtensions
    {
        public static unsafe bool WriteBlitable<T>(this ref DataStreamWriter writer, in T value)
            where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>();

            if (!writer.CanWrite(size))
            {
                return false;
            }

            using NativeArray<byte> tempBuffer = new(size, Allocator.Temp);

            void* ptr = tempBuffer.GetUnsafePtr();

            UnsafeUtility.AsRef<T>(ptr) = value;

            writer.WriteBytes(tempBuffer);

            return true;
        }
    }
}
