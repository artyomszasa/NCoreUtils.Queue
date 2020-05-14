using NCoreUtils.Data.Build;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Queue.Data
{
    public static class QueueModel
    {
        public static DataModelBuilder CreateBuilder()
        {
            var builder = new DataModelBuilder();
            builder.Entity<Entry>(b =>
            {
                b.SetName("media-queue")
                    .SetKey(e => e.Id);
                b.Property(e => e.State)
                    .SetMaxLength(32)
                    .SetUnicode(false)
                    .SetRequired(true)
                    .SetName("state");
                b.Property(e => e.Created)
                    .SetName("created");
                b.Property(e => e.Started)
                    .SetName("started");
                b.Property(e => e.EntryType)
                    .SetMaxLength(32)
                    .SetUnicode(false)
                    .SetRequired(true)
                    .SetName("entryType");
                b.Property(e => e.Source)
                    .SetUnicode(false)
                    .SetRequired(false)
                    .SetName("source");
                b.Property(e => e.Target)
                    .SetUnicode(false)
                    .SetRequired(false)
                    .SetName("target");
                b.Property(e => e.Operation)
                    .SetUnicode(false)
                    .SetRequired(false)
                    .SetName("operation");
                b.Property(e => e.TargetWidth)
                    .SetRequired(false)
                    .SetName("targetWidth");
                b.Property(e => e.TargetHeight)
                    .SetRequired(false)
                    .SetName("targetHeight");
                b.Property(e => e.WeightX)
                    .SetRequired(false)
                    .SetName("weightX");
                b.Property(e => e.WeightY)
                    .SetRequired(false)
                    .SetName("weightY");
                b.Property(e => e.TargetType)
                    .SetUnicode(false)
                    .SetRequired(false)
                    .SetName("targetType");
            });

            return builder;
        }
    }
}