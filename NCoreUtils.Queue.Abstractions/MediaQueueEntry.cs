namespace NCoreUtils.Queue
{
    public class MediaQueueEntry
    {
        public string EntryType { get; }

        public string? Source { get; }

        public string? Target { get; }

        public string? Operation { get; }

        public int? TargetWidth { get; }

        public int? TargetHeight { get; }

        public int? WeightX { get; }

        public int? WeightY { get; }

        public string? TargetType { get; }

        public MediaQueueEntry(
            string entryType,
            string? source = default,
            string? target = default,
            string? operation = default,
            int? targetWidth = default,
            int? targetHeight = default,
            int? weightX = default,
            int? weightY = default,
            string? targetType = default)
        {
            EntryType = entryType ?? MediaQueueEntryTypes.Unknown;
            Source = source;
            Target = target;
            Operation = operation;
            TargetWidth = targetWidth;
            TargetHeight = targetHeight;
            WeightX = weightX;
            WeightY = weightY;
            TargetType = targetType;
        }
    }
}