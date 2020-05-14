using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using NCoreUtils.Memory;

namespace NCoreUtils.Queue
{
    public class MediaQueueEntry : IEmplaceable<MediaQueueEntry>
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

        #if NETSTANDARD2_1
        private bool TryToStringNoAlloc([NotNullWhen(true)] out string? result)
        #else
        private bool TryToStringNoAlloc( out string result)
        #endif
        {
            Span<char> buffer = stackalloc char[8 * 1024];
            if (TryEmplace(buffer, out var size))
            {
                result = buffer.Slice(0, size).ToString();
                return true;
            }
            #if NETSTANDARD2_1
            result = default;
            #else
            result = default!;
            #endif
            return false;
        }

        public int Emplace(Span<char> span)
        {
            if (TryEmplace(span, out var total))
            {
                return total;
            }
            throw new InvalidOperationException($"Provided buffer is too small to emplace {nameof(MediaQueueEntry)}.");
        }

        public bool TryEmplace(Span<char> span, out int used)
        {
            var builder = new SpanBuilder(span);
            used = default;
            if (!builder.TryAppend("[EntryType=")) { return false; }
            if (!builder.TryAppend(EntryType)) { return false; }
            if (null != Source)
            {
                if (!builder.TryAppend(", Source=")) { return false; }
                if (!builder.TryAppend(Source)) { return false; }
            }
            if (null != Target)
            {
                if (!builder.TryAppend(", Target=")) { return false; }
                if (!builder.TryAppend(Target)) { return false; }
            }
            if (null != Operation)
            {
                if (!builder.TryAppend(", Operation=")) { return false; }
                if (!builder.TryAppend(Operation)) { return false; }
            }
            if (TargetWidth.HasValue)
            {
                if (!builder.TryAppend(", TargetWidth=")) { return false; }
                if (!builder.TryAppend(TargetWidth.Value)) { return false; }
            }
            if (TargetHeight.HasValue)
            {
                if (!builder.TryAppend(", TargetHeight=")) { return false; }
                if (!builder.TryAppend(TargetHeight.Value)) { return false; }
            }
            if (WeightX.HasValue)
            {
                if (!builder.TryAppend(", WeightX=")) { return false; }
                if (!builder.TryAppend(WeightX.Value)) { return false; }
            }
            if (WeightY.HasValue)
            {
                if (!builder.TryAppend(", WeightY=")) { return false; }
                if (!builder.TryAppend(WeightY.Value)) { return false; }
            }
            if (null != TargetType)
            {
                if (!builder.TryAppend(", TargetType=")) { return false; }
                if (!builder.TryAppend(TargetType)) { return false; }
            }
            if (!builder.TryAppend(']')) { return false; }
            used = builder.Length;
            return true;
        }

        public override string ToString()
        {
            if (TryToStringNoAlloc(out var result))
            {
                return result;
            }
            var builder = new StringBuilder();
            builder.Append("[EntryType=");
            builder.Append(EntryType);
            if (null != Source)
            {
                builder.Append(", Source=");
                builder.Append(Source);
            }
            if (null != Target)
            {
                builder.Append(", Target=");
                builder.Append(Target);
            }
            if (null != Operation)
            {
                builder.Append(", Operation=");
                builder.Append(Operation);
            }
            if (TargetWidth.HasValue)
            {
                builder.Append(", TargetWidth=");
                builder.Append(TargetWidth.Value);
            }
            if (TargetHeight.HasValue)
            {
                builder.Append(", TargetHeight=");
                builder.Append(TargetHeight.Value);
            }
            if (WeightX.HasValue)
            {
                builder.Append(", WeightX=");
                builder.Append(WeightX.Value);
            }
            if (WeightY.HasValue)
            {
                builder.Append(", WeightY=");
                builder.Append(WeightY.Value);
            }
            if (null != TargetType)
            {
                builder.Append(", TargetType=");
                builder.Append(TargetType);
            }
            builder.Append(']');
            return builder.ToString();
        }
    }
}