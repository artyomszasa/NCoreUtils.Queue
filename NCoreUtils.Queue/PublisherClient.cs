using NCoreUtils.Google.Cloud.PubSub;

namespace NCoreUtils.Queue;

public class PublisherClient(string projectId, string topic, IPubSubV1Api api)
{
    public async Task<string> PublishAsync(string data, CancellationToken cancellationToken = default)
    {
        var response = await api.PublishAsync(projectId, topic, [ new PubSubMessage(data, default!)], cancellationToken).ConfigureAwait(false);
        if (response.MessageIds is { Count: >0 })
        {
            return response.MessageIds[0];
        }
        throw new InvalidOperationException("Pub/Sub API returned no message ID.");
    }
}