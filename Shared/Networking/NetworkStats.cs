using System.Threading;

namespace Shared.Networking
{
    /// <summary>
    /// Provides a centralized, thread-safe mechanism for tracking network statistics.
    /// This class monitors the total bytes sent and received, the number of messages processed,
    /// and calculates the average payload size for both incoming and outgoing traffic.
    /// It is designed as a static class to be easily accessible from different parts of the networking layer.
    /// </summary>
    public static class NetworkStats
    {
        private static long _bytesSent;
        private static long _bytesReceived;
        private static long _messagesSent;
        private static long _messagesReceived;

        public static long BytesSent => _bytesSent;
        public static long BytesReceived => _bytesReceived;
        public static long MessagesSent => _messagesSent;
        public static long MessagesReceived => _messagesReceived;
        public static float AverageSentPayloadSize => _messagesSent == 0 ? 0 : (float)_bytesSent / _messagesSent;
        public static float AverageReceivedPayloadSize => _messagesReceived == 0 ? 0 : (float)_bytesReceived / _messagesReceived;

        public static void RecordMessageSent(long size)
        {
            Interlocked.Add(ref _bytesSent, size);
            Interlocked.Increment(ref _messagesSent);
        }

        public static void RecordMessageReceived(long size)
        {
            Interlocked.Add(ref _bytesReceived, size);
            Interlocked.Increment(ref _messagesReceived);
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref _bytesSent, 0);
            Interlocked.Exchange(ref _bytesReceived, 0);
            Interlocked.Exchange(ref _messagesSent, 0);
            Interlocked.Exchange(ref _messagesReceived, 0);
        }
    }
}
