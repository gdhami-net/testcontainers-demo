using System.Text;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace TestcontainersDemo;

/// <summary>
/// Payload round-trip, acknowledgment, delivery — through RabbitMQ
/// itself, not a List&lt;T&gt; pretending to be a queue.
/// </summary>
public sealed class RabbitMqRoundTripTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:4.3").Build();

    public async Task InitializeAsync() => await _rabbit.StartAsync();

    public async Task DisposeAsync() => await _rabbit.DisposeAsync().AsTask();

    [Fact]
    public async Task Message_round_trips_through_a_real_broker()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_rabbit.GetConnectionString()) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync("orders", durable: false, exclusive: true, autoDelete: true);
        await channel.BasicPublishAsync(exchange: "", routingKey: "orders",
            body: Encoding.UTF8.GetBytes("order-42"));

        BasicGetResult? delivered = null;
        for (var attempt = 0; attempt < 50 && delivered is null; attempt++)
        {
            delivered = await channel.BasicGetAsync("orders", autoAck: true);
            if (delivered is null) await Task.Delay(100);
        }

        Assert.NotNull(delivered);
        Assert.Equal("order-42", Encoding.UTF8.GetString(delivered.Body.ToArray()));
    }
}
