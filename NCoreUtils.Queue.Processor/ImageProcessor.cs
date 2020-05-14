using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;
using NCoreUtils.Images;
using NCoreUtils.Linq;
using NCoreUtils.Queue.Data;

namespace NCoreUtils.Queue
{
    public class ImageProcessor : IHostedService
    {
        private sealed class TaskWrapper : IDisposable
        {
            public CancellationTokenSource Cancellation { get; }

            public Task Task { get; }

            public TaskWrapper(Func<CancellationToken, Task> entryPoint)
            {
                Cancellation = new CancellationTokenSource();
                Task = Task.Factory.StartNew(
                    () => entryPoint(Cancellation.Token),
                    Cancellation.Token,
                    TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                    TaskScheduler.Current
                ).Unwrap();
            }

            public void CancelAndWait(CancellationToken cancellationToken)
            {
                Cancellation.Cancel();
                Task.Wait(cancellationToken);
            }

            public void Dispose()
            {
                Cancellation.Dispose();
            }
        }

        private readonly object _sync = new object();

        private readonly ILogger _logger;

        private readonly IServiceProvider _serviceProvider;

        private TaskWrapper? _task;

        public ImageProcessor(ILogger<ImageProcessor> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        private async Task<(string NewState, string Id)> ProcessEntryAsync(IImageResizer resizer, Entry entry, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(entry.Source))
                {
                    _logger.LogError($"Failed to process {entry.Id}: source is empty.");
                    return (EntryState.Failed, entry.Id);
                }
                if (string.IsNullOrEmpty(entry.Target))
                {
                    _logger.LogError($"Failed to process {entry.Id}: target is empty.");
                    return (EntryState.Failed, entry.Id);
                }
                if (!Uri.TryCreate(entry.Source, UriKind.Absolute, out var sourceUri))
                {
                    _logger.LogError($"Failed to process {entry.Id}: source is not a valid uri [{entry.Source}].");
                    return (EntryState.Failed, entry.Id);
                }
                if (!Uri.TryCreate(entry.Target, UriKind.Absolute, out var targetUri))
                {
                    _logger.LogError($"Failed to process {entry.Id}: target is not a valid uri [{entry.Target}].");
                    return (EntryState.Failed, entry.Id);
                }
                if (sourceUri.Scheme != "gs")
                {
                    _logger.LogError($"Failed to process {entry.Id}: source scheme is not supported [{entry.Source}].");
                    return (EntryState.Failed, entry.Id);
                }
                if (targetUri.Scheme != "gs")
                {
                    _logger.LogError($"Failed to process {entry.Id}: target scheme is not supported [{entry.Source}].");
                    return (EntryState.Failed, entry.Id);
                }
                await resizer.ResizeAsync(
                    new GoogleCloudStorageSource(sourceUri),
                    new GoogleCloudStorageDestination(targetUri),
                    new ResizeOptions(
                        imageType: entry.TargetType,
                        width: entry.TargetWidth,
                        height: entry.TargetHeight,
                        resizeMode: entry.Operation,
                        weightX: entry.WeightX,
                        weightY: entry.WeightY
                    ),
                    cancellationToken
                );
            }
            catch (InvalidImageException exn)
            {
                _logger.LogError(exn, $"Failed to process {entry.Id}: {exn.Message}.");
                return (EntryState.Failed, entry.Id);
            }
            catch (UnsupportedImageTypeException exn)
            {
                _logger.LogError(exn, $"Failed to process {entry.Id}: {exn.Message}.");
                return (EntryState.Failed, entry.Id);
            }
            catch (UnsupportedResizeModeException exn)
            {
                _logger.LogError(exn, $"Failed to process {entry.Id}: {exn.Message}.");
                return (EntryState.Failed, entry.Id);
            }
            catch (InternalImageException exn)
            {
                _logger.LogError(exn, $"Failed to process {entry.Id}: {exn.Message}.");
                return (EntryState.Failed, entry.Id);
            }
            catch (Exception exn)
            {
                _logger.LogError(exn, $"Failed to process {entry.Id}, processing will be tried.");
                return (EntryState.Pending, entry.Id);
            }
            return (EntryState.Done, entry.Id);
        }

        private async Task Run(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IDataRepository<Entry>>();
                        var resizer = scope.ServiceProvider.GetRequiredService<IImageResizer>();
                        var entries = await repository.Items
                            .Where(e => e.State == EntryState.Pending && e.EntryType == MediaQueueEntryTypes.Image)
                            .OrderByDescending(e => e.Created)
                            .Take(12)
                            .ToListAsync(cancellationToken);
                        var results = await Task.WhenAll(entries.Select(e => ProcessEntryAsync(resizer, e, cancellationToken)));
                        foreach (var (newState, id) in results)
                        {
                            var entry = entries.FirstOrDefault(e => e.Id == id);
                            if (null != entry)
                            {
                                await repository.PersistAsync(entry.WithState(newState), CancellationToken.None);
                            }
                        }
                    }
                    // await Task.Delay(TimeSpan.FromSeconds())
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"{nameof(ImageProcessor)} stopped successfully.");
                return;
            }
            catch (Exception exn)
            {
                _logger.LogError(exn, $"{nameof(ImageProcessor)} stopped due to failure.");
                return;
            }
        }

        private Task StopAsync(TaskWrapper task, CancellationToken cancellationToken)
        {
            try
            {
                task.CancelAndWait(cancellationToken);
                return Task.CompletedTask;
            }
            catch (Exception exn)
            {
                _logger.LogError(exn, $"Failed to stop {nameof(ImageProcessor)} normally.");
                return Task.FromException(exn);
            }
            finally
            {
                task.Dispose();
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if (_task is null)
                {
                    _task = new TaskWrapper(Run);
                }
                else
                {
                    _logger.LogWarning($"{nameof(ImageProcessor)} is already running.");
                }
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            TaskWrapper? task;
            lock (_sync)
            {
                task = _task;
                _task = null;
            }
            if (task is null)
            {
                _logger.LogWarning($"{nameof(ImageProcessor)} is not running.");
                return Task.CompletedTask;
            }
            return StopAsync(task, cancellationToken);
        }
    }
}