using System;
using NCoreUtils.Memory;

namespace NCoreUtils.Queue;

public class MediaQueueEntry : ISpanExactEmplaceable
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

    private int GetEmplaceBufferSize()
    {
        var size = "[EntryType=".Length + EntryType.Length;
        if (Source is not null)
        {
            size += ", Source=".Length + Source.Length;
        }
        if (Target is not null)
        {
            size += ", Target=".Length + Target.Length;
        }
        if (Operation is not null)
        {
            size += ", Operation=".Length + Operation.Length;
        }
        if (TargetWidth.HasValue)
        {
            size += ", TargetWidth=".Length + 22;
        }
        if (TargetHeight.HasValue)
        {
            size += ", TargetHeight=".Length + 22;
        }
        if (WeightX.HasValue)
        {
            size += ", WeightX=".Length + 22;
        }
        if (WeightY.HasValue)
        {
            size += ", WeightY=".Length + 22;
        }
        if (TargetType is not null)
        {
            size += ", TargetType=".Length + TargetType.Length;
        }
        return size + 1;
    }

    int ISpanExactEmplaceable.GetEmplaceBufferSize()
        => GetEmplaceBufferSize();

#if NET6_0_OR_GREATER
#else
    bool ISpanEmplaceable.TryGetEmplaceBufferSize(out int minimumBufferSize)
    {
        minimumBufferSize = GetEmplaceBufferSize();
        return true;
    }

    bool ISpanEmplaceable.TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider)
        => TryEmplace(destination, out charsWritten);

    public int Emplace(Span<char> span)
    {
        if (TryEmplace(span, out var total))
        {
            return total;
        }
        throw new InvalidOperationException($"Provided buffer is too small to emplace {nameof(MediaQueueEntry)}.");
    }
#endif

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
        => this.ToStringUsingArrayPool();

    public string ToString(string? format, IFormatProvider? formatProvider)
        => ToString();
}