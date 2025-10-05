using System.Runtime.CompilerServices;
using System.Threading;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Copy of <seealso cref="Collections.Spinner"/>.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct Spinner
    {
        private int state;

        /// <summary>
        /// <inheritdoc cref="Collections.Spinner.Acquire"/>
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
        /// <inheritdoc cref="Collections.Spinner.TryAcquire()"/>
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAcquire()
        {
            // First do a memory load (read) to check if lock is free in order to prevent unnecessary cache missed.
            return Volatile.Read(ref state) == 0 &&
                Interlocked.CompareExchange(ref state, 1, 0) == 0;
        }

        /// <summary>
        /// <inheritdoc cref="Collections.Spinner.TryAcquire(bool)"/>
        /// </summary>
        /// <param name="spin"></param>
        /// <returns></returns>
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
        /// <inheritdoc cref="Collections.Spinner.Release"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            Volatile.Write(ref state, 0);
        }
    }
}
