using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;
using NCoreUtils.Queue.Data;

namespace NCoreUtils.Queue
{
    public class MediaProcessingQueue : IMediaProcessingQueue
    {
        private IDataRepository<Entry> _repository;

        public MediaProcessingQueue(IDataRepository<Entry> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Task EnqueueAsync(IEnumerable<MediaQueueEntry> entries, CancellationToken cancellationToken = default)
            => _repository.Context.TransactedAsync(
                isolationLevel: IsolationLevel.Serializable,
                cancellationToken: cancellationToken,
                action: async () =>
                {
                    foreach (var input in entries)
                    {
                        var entry = new Entry(
                            default!,
                            EntryState.Pending,
                            created: DateTime.Now,
                            default,
                            input.EntryType,
                            input.Source,
                            input.Target,
                            input.Operation,
                            input.TargetWidth,
                            input.TargetHeight,
                            input.WeightX,
                            input.WeightY,
                            input.TargetType
                        );
                        await _repository.PersistAsync(entry, cancellationToken);
                    }
                });
    }
}