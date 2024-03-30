using System.Runtime.ExceptionServices;

namespace NCoreUtils.Queue;

public static class MediaProcessingQueueExtensions
{
    private sealed class SyncGuard
    {
        private readonly List<BulkEnqueueFailure> _entries = [];

        public IReadOnlyList<BulkEnqueueFailure> Entries => _entries;

        public void AddSynced(MediaQueueEntry entry, Exception error)
        {
            lock (_entries)
            {
                _entries.Add(new(entry, error));
            }
        }
    }

    public static async Task<BulkEnqueueResults> EnqueueAsync(
        this IMediaProcessingQueue queue,
        IEnumerable<MediaQueueEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var successCounter = 0;
        var failures = new SyncGuard();
        await Task.WhenAll(entries.Select(async (entry) =>
        {
            try
            {
                await queue.EnqueueAsync(entry, cancellationToken).ConfigureAwait(false);
                Interlocked.Increment(ref successCounter);
            }
            catch (Exception exn)
            {
                failures.AddSynced(entry, exn);
            }
        })).ConfigureAwait(false);
        return new BulkEnqueueResults(successCounter, failures.Entries);
    }

    public static async Task<BulkEnqueueResults> EnqueueAsync(
        this IMediaProcessingQueue queue,
        IEnumerable<MediaQueueEntry> entries,
        int retryCount,
        bool throwWhenFailed,
        CancellationToken cancellationToken = default)
    {
        var res = await queue.EnqueueAsync(entries, cancellationToken).ConfigureAwait(false);
        if (res.FailedCount == 0)
        {
            return res;
        }
        if (retryCount > 0)
        {
            var res1 = await queue
                .EnqueueAsync(res.Failed.Select(tup => tup.Entry), retryCount - 1, false, cancellationToken)
                .ConfigureAwait(false);
            // merge results
            res = new BulkEnqueueResults(res.SucceededCount + res1.SucceededCount, res1.Failed);
        }
        if (res.FailedCount == 0 || !throwWhenFailed)
        {
            return res;
        }
        if (res.FailedCount == 1)
        {
            ExceptionDispatchInfo.Capture(res.Failed[0].Error).Throw();
        }
        throw new AggregateException("Bulk media entry enqueue has failed.", res.Failed.Select(tup => tup.Error));
    }
}