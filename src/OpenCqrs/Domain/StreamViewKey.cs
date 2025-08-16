namespace OpenCqrs.Domain;

public interface IStreamViewKey
{
    string Id { get; }
    string[] EventTypeFilter { get; }
}

public interface IStreamViewKey<TView> : IStreamViewKey where TView : IStreamView;

public static class ViewKeyExtensions
{
    public static string ToDatabaseId(this IStreamViewKey streamViewKey, byte version) => 
        $"{streamViewKey.Id}|v:{version}";

    public static string ToPreviousDatabaseId(this IStreamViewKey streamViewKey, byte version) => 
        $"{streamViewKey.Id}|v:{version - 1}";
}
