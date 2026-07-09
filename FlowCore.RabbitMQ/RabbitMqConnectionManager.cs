using RabbitMQ.Client;
using FlowCore.RabbitMQ.Configuration;

namespace FlowCore.RabbitMQ;

internal sealed class RabbitMqConnectionManager : IDisposable
{
    private readonly RabbitMQOptions _options;
    private IConnection? _connection;
    private readonly object _lock = new();

    public RabbitMqConnectionManager(RabbitMQOptions options)
    {
        _options = options;
    }

    public IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        lock (_lock)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = _options.AutoRecovery,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            return _connection;
        }
    }

    public IModel CreateChannel()
    {
        var connection = GetConnection();
        var channel = connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: _options.ExchangeType,
            durable: true,
            autoDelete: false);

        channel.BasicQos(0, _options.PrefetchCount, false);

        return channel;
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}