using System.Text.Json;

namespace NCoreUtils.Queue;

public static class MediaEntryProcessorExtensions
{
    public static async Task ProcessRequestAsync(this MediaEntryProcessor processor, HttpContext context)
    {
        PubSubRequest req;
        MediaQueueEntry entry;
        try
        {
            req = (await JsonSerializer.DeserializeAsync(context.Request.Body, PubSubSerializerContext.Default.PubSubRequest, context.RequestAborted).ConfigureAwait(false))
                ?? throw new InvalidOperationException("Unable to deserialize Pub/Sub request.");
            var entryData = Convert.FromBase64String(req.Message.Data);
            entry = JsonSerializer.Deserialize(entryData, MediaProcessingQueueSerializerContext.Default.MediaQueueEntry)
                ?? throw new InvalidOperationException("Unable to deserialize Pub/Sub request entry.");
        }
        catch (Exception exn)
        {
            processor.Logger.LogError(exn, "Failed to deserializer pub/sub request message.");
            context.Response.StatusCode = 204; // Message should not be retried...
            return;
        }
        context.Response.StatusCode = await processor.ProcessAsync(entry, req.Message.MessageId, context.RequestAborted).ConfigureAwait(false);
        return;
    }
}