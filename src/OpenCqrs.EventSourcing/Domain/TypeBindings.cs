namespace OpenCqrs.EventSourcing.Domain;

public static class TypeBindings
{
    public static Dictionary<string, Type> DomainEventTypeBindings { get; set; } = new();
    public static Dictionary<string, Type> AggregateTypeBindings { get; set; } = new();

    public static string GetTypeBindingKey(string name, int version) => $"{name}|v:{version}";
}
