using System.Reflection;
using Memoria.EventSourcing.Dcb;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Brings one model's stored snapshot up to date with the events appended since it was written.
/// </summary>
/// <remarks>
/// Through <see cref="IDcbDomainService"/>, where the reading side of these pages goes straight to
/// the tables. Reading wants the row and everything on it, which the store hands back as a model;
/// this is a write, and folding events into a snapshot and persisting it is the store's own
/// operation rather than something a page should reproduce.
/// <para>
/// Reflection, because the model type is not known until someone uploads it and
/// <see cref="IDcbDomainService.UpdateAggregate{T}"/> is generic and constrained to
/// <c>IDcbAggregateRoot, new()</c> — <see cref="IDcbDomainService.UpdateProjection{T}"/> to
/// <c>IDcbProjection, new()</c>. The closed method is built for the type in hand and the
/// <c>Result&lt;T&gt;</c> it returns is unwrapped the same way.
/// </para>
/// <para>
/// Which of the two is called follows from the identifier rather than being asked for: an
/// identifier names one model and only one kind of model, so nothing else could say it as reliably.
/// </para>
/// </remarks>
public static class ModelRefresher
{
    private static readonly MethodInfo UpdateAggregate =
        typeof(IDcbDomainService).GetMethod(nameof(IDcbDomainService.UpdateAggregate))
        ?? throw new InvalidOperationException("IDcbDomainService.UpdateAggregate is missing.");

    private static readonly MethodInfo UpdateProjection =
        typeof(IDcbDomainService).GetMethod(nameof(IDcbDomainService.UpdateProjection))
        ?? throw new InvalidOperationException("IDcbDomainService.UpdateProjection is missing.");

    /// <summary>
    /// Refreshes a model's snapshot.
    /// </summary>
    /// <param name="service">The DCB domain service.</param>
    /// <param name="model">The aggregate or projection type to refresh.</param>
    /// <param name="identifier">An identifier instance addressing it.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// Whether a snapshot was written, and what went wrong if nothing was. Null with no failure is
    /// the store saying there was nothing to bring up to date — no snapshot, and no events inside
    /// the boundary that this model applies — which is an answer rather than a fault.
    /// </returns>
    public static async Task<RefreshedModel> Refresh(
        IDcbDomainService service,
        Type model,
        object identifier,
        CancellationToken cancellationToken = default)
    {
        var update = identifier switch
        {
            IDcbAggregateId => UpdateAggregate,
            IDcbProjectionId => UpdateProjection,
            _ => null
        };

        if (update is null)
        {
            return new RefreshedModel(false,
                $"{identifier.GetType().Name} is not a DCB identifier, so nothing addresses a snapshot to refresh.");
        }

        try
        {
            var task = (Task?)update.MakeGenericMethod(model).Invoke(service, [identifier, cancellationToken]);

            if (task is null)
            {
                return new RefreshedModel(false, "The store returned nothing at all.");
            }

            await task;

            var result = task.GetType().GetProperty(nameof(Task<object>.Result))?.GetValue(task);

            if (result is null)
            {
                return new RefreshedModel(false, "The store returned nothing at all.");
            }

            var resultType = result.GetType();

            if (Read(resultType, result, "IsNotSuccess") is true)
            {
                return new RefreshedModel(false, Describe(Read(resultType, result, "Failure")));
            }

            return new RefreshedModel(Read(resultType, result, "Value") is not null, null);
        }
        catch (TargetInvocationException exception)
        {
            return new RefreshedModel(false, exception.InnerException?.Message ?? exception.Message);
        }
        catch (Exception exception)
        {
            return new RefreshedModel(false, exception.Message);
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

/// <summary>The outcome of a refresh.</summary>
/// <param name="Refreshed">Whether a snapshot was written.</param>
/// <param name="Error">Why it was not, or null when nothing went wrong.</param>
public sealed record RefreshedModel(bool Refreshed, string? Error);
