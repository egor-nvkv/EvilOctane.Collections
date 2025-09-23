using System.Runtime.CompilerServices;
using System.Threading;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Based on <seealso cref="Collections.Spinner"/>.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct Spinner
    {
        private int state;

        /// <summary>
        /// Continually spin until the lock can be acquired.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Acquire()
        {
            for (; ; )
            {
                // Optimistically assume the lock is free on the first try.
                if (Interlocked.CompareExchange(ref state, 1, 0) == 0)
                {
                    return;
                }

                // Wait for lock to be released without generate cache misses.
                while (Volatile.Read(ref state) == 1)
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Try to acquire the lock and immediately return without spinning.
        /// </summary>
        /// <returns><see langword="true"/> if the lock was acquired, <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAcquire()
        {
            // First do a memory load (read) to check if lock is free in order to prevent unnecessary cache missed.
            return Volatile.Read(ref state) == 0 &&
                Interlocked.CompareExchange(ref state, 1, 0) == 0;
        }

        /// <summary>
        /// Try to acquire the lock, and spin only if <paramref name="spin"/> is <see langword="true"/>.
        /// </summary>
        /// <param name="spin">Set to true to spin the lock.</param>
        /// <returns><see langword="true"/> if the lock was acquired, <see langword="false" otherwise./></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAcquire(bool spin)
        {
            if (spin)
            {
                Acquire();
                return true;
            }

            return TryAcquire();
        }

        /// <summary>
        /// Release the lock
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            Volatile.Write(ref state, 0);
        }
    }
}
