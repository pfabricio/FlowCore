namespace FlowCore.RabbitMQ.Configuration;

public class RabbitMQOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "flowcore";
    public string ExchangeType { get; set; } = "topic";
    public string QueuePrefix { get; set; } = "flowcore";
    public ushort PrefetchCount { get; set; } = 10;
    public bool AutoRecovery { get; set; } = true;
}