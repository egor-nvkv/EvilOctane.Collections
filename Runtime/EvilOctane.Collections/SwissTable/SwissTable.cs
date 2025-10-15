using System.Runtime.CompilerServices;

namespace EvilOctane.Collections
{
    public struct SwissTable
    {
        public const int GroupSize = 16;
        public const float MaxLoadFactor = 7 / 8f;

        public const byte ControlEmpty = 0xff;
        public const byte ControlDeleted = 0x80;
        public const byte ControlFullMask = 0x7f;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        /// <remarks>
        /// <seealso href="https://lemire.me/blog/2016/06/27/a-fast-alternative-to-the-modulo-reduction/"/>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Reduce(uint x, uint n)
        {
            return (uint)((x * (ulong)n) >> 32);
        }
    }
}
