using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AblazeForge.DirectiveNetcode.Unity.Extensions.Unsafe
{
    public static class DataStreamReaderUnsafeExtensions
    {
        public static unsafe DataReadResult<T> ReadBlitable<T>(this ref DataStreamReader reader)
            where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>();

            if (!reader.CanRead(size))
            {
                return DataReadResult<T>.Failure();
            }

            byte[] buffer = new byte[size];
            reader.ReadBytes(buffer);

            fixed (byte* ptr = buffer)
            {
                T value = UnsafeUtility.AsRef<T>(ptr);
                return DataReadResult.Success(value);
            }
        }
    }
}
