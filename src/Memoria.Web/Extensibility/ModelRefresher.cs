using System.Reflection;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Brings one model's stored snapshot up to date with the events appended since it was written.
/// </summary>
/// <remarks>
/// Through the store's own domain service, where the reading side of these pages goes straight to
/// the tables. Reading wants the row and everything on it, which the store hands back as a model;
/// this is a write, and folding events into a snapshot and persisting it is the store's own
/// operation rather than something a page should reproduce.
/// <para>
/// Reflection, because the model type is not known until someone uploads it and each store's update
/// methods are generic and constrained — <c>IDcbAggregateRoot, new()</c> and <c>IDcbProjection,
/// new()</c> on the one, <c>IAggregateRoot, new()</c> and <c>IProjection, new()</c> on the other.
/// The closed method is built for the type in hand and the <c>Result&lt;T&gt;</c> it returns is
/// unwrapped the same way.
/// </para>
/// <para>
/// Which of the two is called follows from the identifier rather than being asked for: an
/// identifier names one model and only one kind of model, so nothing else could say it as reliably.
/// </para>
/// <para>
/// The two stores differ only in what addresses a model. A DCB one is named by an identifier alone,
/// which carries its own boundary; a streamed one is folded from a stream through an identifier, so
/// both are handed over and neither says the other.
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

    private static readonly MethodInfo UpdateStreamedAggregate =
        typeof(IDomainService).GetMethod(nameof(IDomainService.UpdateAggregate))
        ?? throw new InvalidOperationException("IDomainService.UpdateAggregate is missing.");

    private static readonly MethodInfo UpdateStreamedProjection =
        typeof(IDomainService).GetMethod(nameof(IDomainService.UpdateProjection))
        ?? throw new InvalidOperationException("IDomainService.UpdateProjection is missing.");

    /// <summary>
    /// Refreshes a DCB model's snapshot.
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
    public static Task<RefreshedModel> Refresh(
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

        return update is null
            ? Task.FromResult(new RefreshedModel(false,
                $"{identifier.GetType().Name} is not a DCB identifier, so nothing addresses a snapshot to refresh."))
            : Invoke(update, model, service, [identifier, cancellationToken]);
    }

    /// <summary>
    /// Refreshes a streamed model's snapshot.
    /// </summary>
    /// <param name="service">The streamed domain service.</param>
    /// <param name="model">The aggregate or projection type to refresh.</param>
    /// <param name="streamId">The stream it is folded from.</param>
    /// <param name="identifier">An identifier instance naming it inside that stream.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// Whether a snapshot was written, and what went wrong if nothing was. Null with no failure is
    /// the store saying there was nothing to bring up to date — no snapshot, and no events in the
    /// stream that this model folds — which is an answer rather than a fault.
    /// </returns>
    /// <remarks>
    /// The stream as well as the identifier, because a streamed model is the fold of one over the
    /// other: a snapshot folded from another stream would be a different model written under this
    /// one's id.
    /// </remarks>
    public static Task<RefreshedModel> Refresh(
        IDomainService service,
        Type model,
        object streamId,
        object identifier,
        CancellationToken cancellationToken = default)
    {
        if (streamId is not IStreamId)
        {
            return Task.FromResult(new RefreshedModel(false,
                $"{streamId.GetType().Name} is not a stream, so there is nowhere to fold a snapshot from."));
        }

        var update = identifier switch
        {
            IAggregateId => UpdateStreamedAggregate,
            IProjectionId => UpdateStreamedProjection,
            _ => null
        };

        return update is null
            ? Task.FromResult(new RefreshedModel(false,
                $"{identifier.GetType().Name} is not a streamed identifier, so nothing addresses a snapshot to refresh."))
            : Invoke(update, model, service, [streamId, identifier, cancellationToken]);
    }

    /// <summary>
    /// Closes one of the store's update methods over the model in hand, calls it, and reads what
    /// comes back.
    /// </summary>
    /// <remarks>
    /// The generic method is closed inside the guard rather than before it: a model that does not
    /// satisfy the constraint — an aggregate without a constructor taking nothing, say — throws
    /// here, and that is a page's answer rather than the application's.
    /// </remarks>
    private static async Task<RefreshedModel> Invoke(
        MethodInfo update, Type model, object service, object?[] arguments)
    {
        try
        {
            var task = (Task?)update.MakeGenericMethod(model).Invoke(service, arguments);

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
