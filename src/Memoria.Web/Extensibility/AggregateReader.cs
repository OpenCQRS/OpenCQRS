using System.Reflection;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Asks the DCB store for one aggregate, given an identifier built at runtime.
/// </summary>
/// <remarks>
/// Everything here is reflection because the aggregate type is not known until someone uploads it.
/// <see cref="IDcbDomainService.GetAggregate{T}"/> is generic and constrained to
/// <c>IDcbAggregateRoot, new()</c>, so the closed method is built for the type in hand and the
/// <c>Result&lt;T&gt;</c> it returns is unwrapped the same way.
/// </remarks>
public static class AggregateReader
{
    private static readonly MethodInfo GetAggregate =
        typeof(IDcbDomainService).GetMethod(nameof(IDcbDomainService.GetAggregate))
        ?? throw new InvalidOperationException("IDcbDomainService.GetAggregate is missing.");

    /// <summary>
    /// Loads an aggregate.
    /// </summary>
    /// <param name="service">The DCB domain service.</param>
    /// <param name="aggregate">The aggregate type to load.</param>
    /// <param name="identifier">An identifier instance addressing it.</param>
    /// <param name="readMode">How the snapshot and any newer events should be combined.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<LoadedAggregate> Load(
        IDcbDomainService service,
        Type aggregate,
        object identifier,
        ReadMode readMode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var task = (Task?)GetAggregate.MakeGenericMethod(aggregate)
                .Invoke(service, [identifier, readMode, cancellationToken]);

            if (task is null)
            {
                return new LoadedAggregate(null, "The store returned nothing at all.");
            }

            await task;

            var result = task.GetType().GetProperty(nameof(Task<object>.Result))?.GetValue(task);

            if (result is null)
            {
                return new LoadedAggregate(null, "The store returned nothing at all.");
            }

            var resultType = result.GetType();

            if (Read(resultType, result, "IsNotSuccess") is true)
            {
                return new LoadedAggregate(null, Describe(Read(resultType, result, "Failure")));
            }

            // Null with no failure is the store saying this aggregate is not there, which is an
            // answer rather than a fault — the read mode asked not to create one.
            return new LoadedAggregate(Read(resultType, result, "Value"), null);
        }
        catch (TargetInvocationException exception)
        {
            return new LoadedAggregate(null, exception.InnerException?.Message ?? exception.Message);
        }
        catch (Exception exception)
        {
            return new LoadedAggregate(null, exception.Message);
        }
    }

    /// <summary>
    /// Reads one property off a result.
    /// </summary>
    /// <remarks>
    /// Declared properties first: <c>Result&lt;T&gt;.Value</c> hides a base member of the same name,
    /// and asking for it by name alone is an ambiguous match rather than the one wanted.
    /// </remarks>
    private static object? Read(Type type, object instance, string name)
    {
        var property =
            type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            ?? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(candidate => candidate.Name == name);

        return property?.GetValue(instance);
    }

    private static string Describe(object? failure)
    {
        if (failure is null)
        {
            return "The store reported a failure with no detail.";
        }

        var type = failure.GetType();
        var title = type.GetProperty("Title")?.GetValue(failure) as string;
        var description = type.GetProperty("Description")?.GetValue(failure) as string;

        return string.Join(" — ", new[] { title, description }.Where(part => !string.IsNullOrWhiteSpace(part)))
            is { Length: > 0 } message
            ? message
            : failure.ToString() ?? "The store reported a failure.";
    }
}

/// <summary>The outcome of a load.</summary>
/// <param name="Aggregate">The aggregate, or null when it was not found or the load failed.</param>
/// <param name="Error">Why the load failed, or null when it did not.</param>
public sealed record LoadedAggregate(object? Aggregate, string? Error);
