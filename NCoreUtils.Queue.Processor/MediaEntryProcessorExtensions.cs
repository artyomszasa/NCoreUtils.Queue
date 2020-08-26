using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NCoreUtils.Queue.Internal;

namespace NCoreUtils.Queue
{
    public static class MediaEntryProcessorExtensions
    {
        static private readonly JsonSerializerOptions requestSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static private readonly JsonSerializerOptions entrySerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { MediaQueueEntryConverter.Instance }
        };

        public static async Task ProcessRequestAsync(this MediaEntryProcessor processor, HttpContext context)
        {
            PubSubRequest req;
            MediaQueueEntry entry;
            try
            {
                req = await JsonSerializer.DeserializeAsync<PubSubRequest>(context.Request.Body, requestSerializerOptions, context.RequestAborted);
                entry = JsonSerializer.Deserialize<MediaQueueEntry>(Convert.FromBase64String(req.Message.Data), entrySerializerOptions);
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