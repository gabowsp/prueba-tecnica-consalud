using System.Globalization;
using System.Text.Json;
using RabbitMQ.Client;
using PruebaTecnica.Application.Ports.Output;

namespace PruebaTecnica.Infrastructure.Messaging;

public sealed class RabbitMqPesosEspecialesPublisher(RabbitMqOptions options)
    : IPesosEspecialesPublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IConnection? connection;
    private IChannel? channel;

    public async Task PublicarAsync(long id, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (connection?.IsOpen != true || channel?.IsOpen != true)
            {
                await ResetAsync();
                var factory = new ConnectionFactory
                {
                    HostName = options.HostName, Port = options.Port,
                    UserName = options.UserName, Password = options.Password,
                    VirtualHost = options.VirtualHost,
                    AutomaticRecoveryEnabled = false,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
                };
                connection = await factory.CreateConnectionAsync(cancellationToken);
                channel = await connection.CreateChannelAsync(
                    new CreateChannelOptions(publisherConfirmationsEnabled: true,
                        publisherConfirmationTrackingEnabled: true), cancellationToken);
                // Usa la cola durable existente sin cambiar su configuración.
                await channel.QueueDeclarePassiveAsync(options.Queue, cancellationToken);
            }
            var properties = new BasicProperties
            {
                ContentType = "application/json", Persistent = true,
                MessageId = "peso-especial-" + id.ToString(CultureInfo.InvariantCulture)
            };
            await channel!.BasicPublishAsync(exchange: "", routingKey: options.Queue,
                mandatory: true, basicProperties: properties,
                body: JsonSerializer.SerializeToUtf8Bytes(new { id }),
                cancellationToken: cancellationToken);
        }
        catch
        {
            await ResetAsync();
            throw;
        }
        finally { gate.Release(); }
    }

    private async Task ResetAsync()
    {
        var oldChannel = channel; var oldConnection = connection;
        channel = null; connection = null;
        try { if (oldChannel is not null) await oldChannel.DisposeAsync(); }
        finally { if (oldConnection is not null) await oldConnection.DisposeAsync(); }
    }

    public async ValueTask DisposeAsync()
    {
        await ResetAsync();
        gate.Dispose();
    }
}
