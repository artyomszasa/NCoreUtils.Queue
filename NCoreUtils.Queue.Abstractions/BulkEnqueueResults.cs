namespace NCoreUtils.Queue;

public readonly struct BulkEnqueueFailure(MediaQueueEntry entry, Exception error)
{
    public MediaQueueEntry Entry { get; } = entry;

    public Exception Error { get; } = error;

    public void Desconstruct(out MediaQueueEntry entry, out Exception error)
    {
        entry = Entry;
        error = Error;
    }
}

public class BulkEnqueueResults(int succeededCount, IReadOnlyList<BulkEnqueueFailure> failed)
{
    public int SucceededCount { get; } = succeededCount;

    public IReadOnlyList<BulkEnqueueFailure> Failed { get; } = failed ?? throw new ArgumentNullException(nameof(failed));

    public int FailedCount => Failed.Count;

    public int TotalProcessed => SucceededCount + FailedCount;
}