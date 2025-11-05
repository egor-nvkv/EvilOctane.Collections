using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities
{
    [GenerateTestsForBurstCompatibility]
    public static unsafe class LogUtility
    {
        public const int MaxTagLength = 128;

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LogTagged(ByteSpan primaryTag, ByteSpan secondaryTag, ByteSpan message, LogType logType)
        {
            SkipInit(out FixedString4096Bytes log);

            bool hasPrimaryTag = !primaryTag.IsEmpty;
            bool hasSecondaryTag = !secondaryTag.IsEmpty;

            ByteSpan primaryTagTruncated = primaryTag[..MaxTagLength];
            ByteSpan secondaryTagTruncated = secondaryTag[..MaxTagLength];

            int prologueLength =
                primaryTagTruncated.Length + (hasPrimaryTag ? 1/* */ : 0) +
                secondaryTagTruncated.Length + (hasSecondaryTag ? 3/*[] */ : 0) +
                2/*| */;

            ByteSpan messageTruncated = message[..(log.Capacity - prologueLength)];
            int totalLength = prologueLength + messageTruncated.Length;

            log.Length = totalLength;
            int offset = 0;

            // Primary tag
            if (hasPrimaryTag)
            {
                new ByteSpan(log.GetUnsafePtr() + offset, primaryTagTruncated.Length).CopyFrom(primaryTagTruncated);
                offset += primaryTagTruncated.Length;

                log[offset++] = (byte)' ';
            }

            // Secondary tag
            if (hasSecondaryTag)
            {
                log[offset++] = (byte)'[';

                new ByteSpan(log.GetUnsafePtr() + offset, secondaryTagTruncated.Length).CopyFrom(secondaryTagTruncated);
                offset += secondaryTagTruncated.Length;

                log[offset++] = (byte)']';
                log[offset++] = (byte)' ';
            }

            // Separator
            log[offset++] = (byte)'|';
            log[offset++] = (byte)' ';

            // Message
            new ByteSpan(log.GetUnsafePtr() + offset, messageTruncated.Length).CopyFrom(messageTruncated);

            switch (logType)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    Debug.LogError(log);
                    break;

                case LogType.Warning:
                    Debug.LogWarning(log);
                    break;

                default:
                case LogType.Log:
                    Debug.Log(log);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogTaggedGeneric<S0, S1>(in S0 primaryTag, in S1 message, LogType logType = LogType.Log)
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            LogTagged(
                AsRef(in primaryTag).AsByteSpan(),
                ByteSpan.Empty,
                AsRef(in message).AsByteSpan(),
                logType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogTaggedGeneric<S0, S1, S2>(in S0 primaryTag, in S1 secondaryTag, in S2 message, LogType logType = LogType.Log)
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            LogTagged(
                AsRef(in primaryTag).AsByteSpan(),
                AsRef(in secondaryTag).AsByteSpan(),
                AsRef(in message).AsByteSpan(),
                logType);
        }
    }
}
