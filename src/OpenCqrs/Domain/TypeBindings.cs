namespace OpenCqrs.Domain;

public static class TypeBindings
{
    public static Dictionary<string, Type> DomainEventBindings { get; set; } = new();
    public static Dictionary<string, Type> StreamViewBindings { get; set; } = new();
    
    public static string GetTypeBindingKey(string name, int version) => $"{name}|v:{version}";
}
