using System.Text.Json.Serialization;
using NCoreUtils.Memory;

namespace NCoreUtils.Queue;

public class MediaQueueEntry(
    string entryType,
    string? source = default,
    string? target = default,
    string? operation = default,
    int? targetWidth = default,
    int? targetHeight = default,
    int? weightX = default,
    int? weightY = default,
    string? targetType = default)
    : ISpanExactEmplaceable
{
    public string EntryType { get; } = entryType ?? MediaQueueEntryTypes.Unknown;

    public string? Source { get; } = source;

    public string? Target { get; } = target;

    public string? Operation { get; } = operation;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? TargetWidth { get; } = targetWidth;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? TargetHeight { get; } = targetHeight;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? WeightX { get; } = weightX;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? WeightY { get; } = weightY;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? TargetType { get; } = targetType;

    #region emplaceable

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

    #endregion
}