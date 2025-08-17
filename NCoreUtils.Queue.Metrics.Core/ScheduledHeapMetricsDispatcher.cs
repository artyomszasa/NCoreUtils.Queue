using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NCoreUtils.Google.Cloud.Monitoring;

namespace NCoreUtils.Queue;

public class ScheduledHeapMetricsDispatcher(
    ILogger<ScheduledHeapMetricsDispatcher> logger,
    IMonitoringV3Api api,
    string projectId,
    MonitoredResource resource,
    TimeSpan? delay = default)
{
    private static Metric HeapSize { get; } = new("custom.googleapis.com/dotnet/heapSize");

    private static Metric MemoryLoad { get; } = new("custom.googleapis.com/dotnet/memoryLoad");

    private static Metric TotalCommitted { get; } = new("custom.googleapis.com/dotnet/totalCommitted");

    private static Metric TotalAvailable { get; } = new("custom.googleapis.com/dotnet/totalAvailable");

    private static Metric Fragmented { get; } = new("custom.googleapis.com/dotnet/fragmented");

    private static async Task<string> GetMetadata(IHttpClientFactory httpClientFactory, string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Metadata-Flavor", "Google");
        using var client = httpClientFactory.CreateClient();
        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public static async Task<MonitoredResource> FetchCurrentResourceDataAsync(
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken = default)
    {
        var projectId = await GetMetadata(httpClientFactory, "http://metadata.google.internal/computeMetadata/v1/project/project-id", cancellationToken);
        var zone = await GetMetadata(httpClientFactory, "http://metadata.google.internal/computeMetadata/v1/instance/zone", cancellationToken);
        var location = zone.LastIndexOf('/') switch
        {
            -1 => zone,
            var ix => zone[(ix + 1)..]
        };
        var clusterName = await GetMetadata(httpClientFactory, "http://metadata.google.internal/computeMetadata/v1/instance/attributes/cluster-name", cancellationToken);
        var namespaceName = Environment.GetEnvironmentVariable("K8S_POD_NAMESPACE");
        var podName = Environment.GetEnvironmentVariable("K8S_POD_NAME");
        var containerName = Environment.GetEnvironmentVariable("K8S_CONTAINER_NAME");
        return MonitoredResource.K8sContainer(projectId, location, clusterName, namespaceName ?? string.Empty, podName ?? string.Empty, containerName ?? string.Empty);
    }

    private readonly TimeSpan Delay = delay ?? TimeSpan.FromSeconds(15);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TimeSeries CreateTimeSeries(Metric metric, DateTimeOffset now, long value) => new(
        metric: metric,
        resource: resource,
        metricKind: MetricKinds.Gauge,
        point: new(now, value)
    );

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var info = GC.GetGCMemoryInfo();
                long heapSize = info.HeapSizeBytes;
                long memoryLoad = info.MemoryLoadBytes;
                long totalCommitted = info.TotalCommittedBytes;
                long totalAvailable = info.TotalAvailableMemoryBytes;
                long fragmented = info.FragmentedBytes;
                // info.PinnedObjectsCount;
                try
                {
                    var now = DateTimeOffset.Now;
                    await api.CreateTimeSeriesAsync(projectId,
                    [
                        CreateTimeSeries(HeapSize, now, heapSize),
                        CreateTimeSeries(MemoryLoad, now, memoryLoad),
                        CreateTimeSeries(TotalCommitted, now, totalCommitted),
                        CreateTimeSeries(TotalAvailable, now, totalAvailable),
                        CreateTimeSeries(Fragmented, now, fragmented)
                    ], CancellationToken.None).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { /* noop */ }
                catch (Exception exn)
                {
                    logger.LogError(exn, "failed to dispatch heap metrics.");
                }
                await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* noop */ }
            catch (Exception exn)
            {
                logger.LogError(exn, "failed to dispatch heap metrics.");
            }
        }
    }
}