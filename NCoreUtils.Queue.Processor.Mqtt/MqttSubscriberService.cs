using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;

namespace NCoreUtils.Queue;

public class MqttSubscriberService : IHostedService
{
    private readonly SemaphoreSlim _sync = new(1);

    private readonly MediaEntryProcessor _processor;

    private readonly ILogger<MqttSubscriberService> _logger;

    private readonly MqttClientOptions _clientOptions;

    private readonly IMqttClientServiceOptions _serviceOptions;

    private IMqttClient? _client;

    private volatile bool _connected;

    public MqttSubscriberService(
        MediaEntryProcessor processor,
        ILogger<MqttSubscriberService> logger,
        IMqttClientServiceOptions serviceOptions,
        MqttClientOptions clientOptions)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
        _serviceOptions = serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "JSON serialization using explicit converter.")]
    public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        try
        {
            var payloadSegment = eventArgs.ApplicationMessage.PayloadSegment;
            if (payloadSegment.Array is null)
            {
                throw new InvalidOperationException("Payload is null.");
            }
            await using var buffer = new MemoryStream(payloadSegment.Array, payloadSegment.Offset, payloadSegment.Count, writable: false, publiclyVisible: true);
            var entry = await JsonSerializer
                .DeserializeAsync(buffer, MediaProcessingQueueSerializerContext.Default.MediaQueueEntry, CancellationToken.None)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Unable to deserialize Pub/Sub request entry.");
            var status = await _processor.ProcessAsync(entry, "<none>", CancellationToken.None).ConfigureAwait(false);
            eventArgs.ProcessingFailed = status >= 400;
        }
        catch (Exception exn)
        {
            _logger.LogError(exn, "Failed to process entry.");
            eventArgs.ProcessingFailed = true;
        }
    }

    private async Task DoConnectAsync(IMqttClient client, CancellationToken cancellationToken)
    {
        await client.ConnectAsync(_clientOptions, cancellationToken);
    }

    public async Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
    {
        _logger.LogDebug(
            "MQTT client created and connected successfully (result code = {ResultCode}, response = {ResponseInformation}).",
            eventArgs.ConnectResult.ResultCode,
            eventArgs.ConnectResult.ResponseInformation
        );
        await _client!.SubscribeAsync(new MqttClientSubscribeOptions
        {
            TopicFilters = {
                new MqttTopicFilterBuilder()
                    .WithAtLeastOnceQoS()
                    .WithTopic(_serviceOptions.Topic)
                    .Build()
            }
        }).ConfigureAwait(false);
        _logger.LogDebug(
            "MQTT client successfully subscribed to topic {Topic}.",
            _serviceOptions.Topic
        );
        _connected = true;
    }

    public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
    {
        _connected = false;
        Interlocked.MemoryBarrierProcessWide();
        if (_client is not null && eventArgs.Reason != MqttClientDisconnectReason.AdministrativeAction && !_connected)
        {
            _logger.LogWarning(eventArgs.Exception, "MQTT client has disconnected, reason: {Reason}, trying to reconnect.", eventArgs.Reason);
            await DoConnectAsync(_client, CancellationToken.None).ConfigureAwait(false);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_client is null)
            {
                var client = new MqttFactory().CreateMqttClient();
                client.ConnectedAsync += HandleConnectedAsync;
                client.DisconnectedAsync += HandleDisconnectedAsync;
                client.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
                _client = client;
                await DoConnectAsync(client, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("MQTT service started successfully.");
            }
            else
            {
                _logger.LogWarning("MQTT service is already running.");
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_client is null)
            {
                _logger.LogWarning("MQTT service is not running.");
            }
            else
            {
                await _client.DisconnectAsync(new MqttClientDisconnectOptions
                {
                    Reason = MqttClientDisconnectOptionsReason.AdministrativeAction,
                    ReasonString = "shutdown"
                }, cancellationToken).ConfigureAwait(false);
                _client = default;
                _logger.LogDebug("MQTT stopped successfully.");
            }
        }
        finally
        {
            _sync.Release();
        }
    }
}