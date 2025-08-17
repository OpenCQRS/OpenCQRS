using OpenCqrs.Domain;

namespace OpenCqrs.Data;

public interface IBindableEntity
{
    string TypeName { get; set; }
    int TypeVersion { get; set; }
}

public static class BindableEntityExtensions
{
    public static string ToBindingKey(this IBindableEntity bindableEntity) => 
        TypeBindings.GetTypeBindingKey(bindableEntity.TypeName, bindableEntity.TypeVersion);
}
