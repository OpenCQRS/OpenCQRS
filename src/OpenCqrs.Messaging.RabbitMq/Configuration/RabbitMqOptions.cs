namespace OpenCqrs.Messaging.RabbitMq.Configuration;

public class RabbitMqOptions
{
    public required string ConnectionString { get; set; }
    public string VirtualHost { get; set; } = "/";
    public int RequestedConnectionTimeout { get; set; } = 60000;
    public int RequestedHeartbeat { get; set; } = 60;
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public bool TopologyRecoveryEnabled { get; set; } = true;
    public string DefaultExchangeName { get; set; } = "amq.topic";
    public string DelayedExchangeName { get; set; } = "delayed_exchange";
    public bool CreateDelayedExchange { get; set; } = true;
}
