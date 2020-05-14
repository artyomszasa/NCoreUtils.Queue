using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Queue
{
    public interface IMediaProcessingQueue
    {
        Task EnqueueAsync(MediaQueueEntry entry, CancellationToken cancellationToken = default);
    }
}