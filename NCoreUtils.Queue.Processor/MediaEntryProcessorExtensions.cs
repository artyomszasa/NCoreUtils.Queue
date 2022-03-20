using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NCoreUtils.Queue.Internal;

namespace NCoreUtils.Queue
{
    public static class MediaEntryProcessorExtensions
    {
        static private readonly JsonSerializerOptions entrySerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { MediaQueueEntryConverter.Instance }
        };

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "JSON serialization using explicit converter.")]
        public static async Task ProcessRequestAsync(this MediaEntryProcessor processor, HttpContext context)
        {
            PubSubRequest req;
            MediaQueueEntry entry;
            try
            {
                req = (await JsonSerializer.DeserializeAsync(context.Request.Body, MediaSerializerContext.Default.PubSubRequest, context.RequestAborted))
                    ?? throw new InvalidOperationException("Unable to deserialize Pub/Sub request.");
                entry = JsonSerializer.Deserialize<MediaQueueEntry>(Convert.FromBase64String(req.Message.Data), entrySerializerOptions)
                    ?? throw new InvalidOperationException("Unable to deserialize Pub/Sub request entry.");
            }
            catch (Exception exn)
            {
                processor.Logger.LogError(exn, "Failed to deserializer pub/sub request message.");
                context.Response.StatusCode = 204; // Message should not be retried...
                return;
            }
            context.Response.StatusCode = await processor.ProcessAsync(entry, req.Message.MessageId, context.RequestAborted);
            return;
        }
    }
}