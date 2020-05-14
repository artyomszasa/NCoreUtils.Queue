using System;
using NCoreUtils.Data;

namespace NCoreUtils.Queue.Data
{
    public class Entry : IHasId<string>
    {
        public string Id { get; } = string.Empty;

        public string State { get; } = EntryState.Pending;

        public DateTimeOffset Created { get; }

        public DateTimeOffset? Started { get; }

        public string EntryType { get; } = "unknown";

        public string? Source { get; }

        public string? Target { get; }

        public string? Operation { get; }

        public int? TargetWidth { get; }

        public int? TargetHeight { get; }

        public int? WeightX { get; }

        public int? WeightY { get; }

        public string? TargetType { get; }

        public Entry(
            string id,
            string state,
            DateTimeOffset created,
            DateTimeOffset? started,
            string entryType,
            string? source,
            string? target,
            string? operation,
            int? targetWidth,
            int? targetHeight,
            int? weightX,
            int? weightY,
            string? targetType)
        {
            Id = id;
            State = state;
            Created = created;
            Started = started;
            EntryType = entryType;
            Source = source;
            Target = target;
            Operation = operation;
            TargetWidth = targetWidth;
            TargetHeight = targetHeight;
            WeightX = weightX;
            WeightY = weightY;
            TargetType = targetType;
        }

        public Entry WithState(string state)
            => new Entry(
                Id,
                state,
                Created,
                Started,
                EntryType,
                Source,
                Target,
                Operation,
                TargetWidth,
                TargetHeight,
                WeightX,
                WeightY,
                TargetType);
    }
}