using System;
using System.Collections.Generic;

namespace NCoreUtils.Queue
{
    public class BulkEnqueueResults
    {
        public int SucceededCount { get; }

        public IReadOnlyList<(MediaQueueEntry Entry, Exception Error)> Failed { get; }

        public int FailedCount => Failed.Count;

        public int TotalProcessed => SucceededCount + FailedCount;

        public BulkEnqueueResults(int succeededCount, IReadOnlyList<(MediaQueueEntry Entry, Exception Error)> failed)
        {
            SucceededCount = succeededCount;
            Failed = failed ?? throw new ArgumentNullException(nameof(failed));
        }
    }
}