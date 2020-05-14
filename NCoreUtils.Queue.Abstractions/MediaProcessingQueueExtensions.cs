using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Queue
{
    public static class MediaProcessingQueueExtensions
    {
        private sealed class SyncGuard
        {
            private readonly List<(MediaQueueEntry Entry, Exception Error)> _entries = new List<(MediaQueueEntry Entry, Exception Error)>();

            public IReadOnlyList<(MediaQueueEntry Entry, Exception Error)> Entries => _entries;

            public void AddSynced(MediaQueueEntry entry, Exception error)
            {
                lock (_entries)
                {
                    _entries.Add((entry, error));
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
                    await queue.EnqueueAsync(entry, cancellationToken);
                    Interlocked.Increment(ref successCounter);
                }
                catch (Exception exn)
                {
                    failures.AddSynced(entry, exn);
                }
            }));
            return new BulkEnqueueResults(successCounter, failures.Entries);
        }

        public static async Task<BulkEnqueueResults> EnqueueAsync(
            this IMediaProcessingQueue queue,
            IEnumerable<MediaQueueEntry> entries,
            int retryCount,
            bool throwWhenFailed,
            CancellationToken cancellationToken = default)
        {
            var res = await queue.EnqueueAsync(entries, cancellationToken);
            if (res.FailedCount == 0)
            {
                return res;
            }
            if (retryCount > 0)
            {
                var res1 = await queue.EnqueueAsync(res.Failed.Select(tup => tup.Entry), retryCount - 1, false, cancellationToken);
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
}