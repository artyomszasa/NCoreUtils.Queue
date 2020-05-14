using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Queue
{
    public interface IMediaProcessingQueue
    {
        Task EnqueueAsync(IEnumerable<MediaQueueEntry> entries, CancellationToken cancellationToken = default);
    }
}