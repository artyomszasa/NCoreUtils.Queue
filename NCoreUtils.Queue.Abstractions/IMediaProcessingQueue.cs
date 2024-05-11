namespace NCoreUtils.Queue;

public interface IMediaProcessingQueue
{
    Task EnqueueAsync(MediaQueueEntry entry, CancellationToken cancellationToken = default);

    Task EnqueueMultipleAsync(IReadOnlyList<MediaQueueEntry> entries, CancellationToken cancellationToken = default)
#if NET5_0_OR_GREATER
        => MediaProcessingQueueExtensions.EnqueueAsync(
            this,
            entries,
            retryCount: 4,
            throwWhenFailed: true,
            cancellationToken: cancellationToken
        );
#else
        ;
#endif
}