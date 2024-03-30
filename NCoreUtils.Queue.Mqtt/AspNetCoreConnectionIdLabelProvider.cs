using NCoreUtils.Logging;
using NCoreUtils.Logging.Google;

namespace NCoreUtils.Queue;

internal sealed class AspNetCoreConnectionIdLabelProvider : ILabelProvider
{
    public void UpdateLabels(
        string category,
        EventId eventId,
        LogLevel logLevel,
        in WebContext context,
        IDictionary<string, string> labels)
    {
        if (!string.IsNullOrEmpty(context.ConnectionId))
        {
            labels.Add("aspnetcore-connection-id", context.ConnectionId);
        }
    }
}