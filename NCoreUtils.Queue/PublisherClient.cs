using NCoreUtils.Google.Cloud.PubSub;

namespace NCoreUtils.Queue;

public class PublisherClient(string projectId, string topic, IPubSubV1Api api)
{
    private static PubSubMessage CreateMessage(string data)
        => new(data, default);

    public async Task<string> PublishAsync(string data, CancellationToken cancellationToken = default)
    {
        var response = await api.PublishAsync(projectId, topic, [ CreateMessage(data) ], cancellationToken).ConfigureAwait(false);
        if (response.MessageIds is { Count: >0 })
        {
            return response.MessageIds[0];
        }
        throw new InvalidOperationException("Pub/Sub API returned no message ID.");
    }

    public async Task<IReadOnlyList<string>> PublishAsync(IEnumerable<string> data, CancellationToken cancellationToken = default)
    {
        var response = await api.PublishAsync(projectId, topic, [ ..data.Select(CreateMessage) ], cancellationToken).ConfigureAwait(false);
        if (response.MessageIds is not null)
        {
            return response.MessageIds;
        }
        throw new InvalidOperationException("Pub/Sub API returned no message IDs.");
    }
}