using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Queue
{
    public static class MediaProcessingQueueExtensions
    {
        public static Task EnqueueAsync(this IMediaProcessingQueue queue, MediaQueueEntry entry, CancellationToken cancellationToken = default)
            => queue.EnqueueAsync(new [] { entry }, cancellationToken);
    }
}