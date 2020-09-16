using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;

namespace NCoreUtils.Queue
{
    public class MqttClientService : IMqttClientService
    {
        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1);

        private readonly ILogger<MqttClientService> _logger;

        private readonly IMqttClientOptions _clientOptions;

        private readonly IMqttClientServiceOptions _serviceOptions;

        private IMqttClient? _client;

        private volatile bool _connected;

        public MqttClientService(
            ILogger<MqttClientService> logger,
            IMqttClientServiceOptions serviceOptions,
            IMqttClientOptions clientOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
            _serviceOptions = serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions));
        }

        public Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            _logger.LogDebug(
                "MQTT client created and connected successfully (result code = {0}, response = {1}).",
                eventArgs.AuthenticateResult.ResultCode,
                eventArgs.AuthenticateResult.ResponseInformation
            );
            _connected = true;
            return Task.CompletedTask;
        }

        public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            _connected = false;
            Interlocked.MemoryBarrierProcessWide();
            if (!(_client is null) && eventArgs.ReasonCode != MqttClientDisconnectReason.AdministrativeAction && !_connected)
            {
                _logger.LogWarning(eventArgs.Exception, "MQTT client has deisconnected, reason: {0}, trying to reconnect.", eventArgs.ReasonCode);
                await _client.ConnectAsync(_clientOptions, CancellationToken.None);
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
                    client.ConnectedHandler = this;
                    client.DisconnectedHandler = this;
                    await client.ConnectAsync(_clientOptions, cancellationToken);
                    _client = client;
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
                        ReasonCode = MqttClientDisconnectReason.AdministrativeAction,
                        ReasonString = "shutdown"
                    });
                    _client = default;
                    _logger.LogDebug("MQTT stopped successfully.");
                }
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<int?> PublishAsync<T>(T payload, CancellationToken cancellationToken)
        {
            if (_client is null)
            {
                throw new InvalidOperationException("MQTT service has not been started properly.");
            }
            if (!_connected)
            {
                throw new InvalidOperationException("MQTT client is not connected.");
            }
            var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
            try
            {
                using var stream = new MemoryStream(buffer, 0, buffer.Length, true, true);
                await JsonSerializer.SerializeAsync(stream, payload, _serviceOptions.JsonSerializerOptions, cancellationToken);
                stream.Seek(0, SeekOrigin.Begin);
                var message = new MqttApplicationMessageBuilder()
                    .WithAtLeastOnceQoS()
                    .WithPayload(stream)
                    .WithTopic(_serviceOptions.Topic)
                    .Build();
                var res = await _client.PublishAsync(message, cancellationToken);
                if (res.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new InvalidOperationException($"Message publishing failed with reason: {res.ReasonCode} {res.ReasonString}");
                }
                return res.PacketIdentifier;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}