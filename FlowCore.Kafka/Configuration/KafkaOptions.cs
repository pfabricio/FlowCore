namespace FlowCore.Kafka.Configuration;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroup { get; set; } = "flowcore";
    public string TopicPrefix { get; set; } = "flowcore";
    public bool EnableAutoCommit { get; set; } = false;
    public int MaxPollIntervalMs { get; set; } = 300000;
}