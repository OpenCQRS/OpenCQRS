using System.Reflection;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Calls one of a store's generic operations for a model type that was not known until someone
/// uploaded it, and reads the <c>Result&lt;T&gt;</c> that comes back.
/// </summary>
/// <remarks>
/// Shared by the refresh and the fold, which differ only in which method they close and what they
/// make of the value: everything between — closing the method, awaiting the task, telling a
/// success from the store's own failure, and a store that threw from either — is the same read.
/// </remarks>
internal static class StoreCall
{
    /// <summary>
    /// Closes the operation over the model in hand, calls it, and reads what comes back.
    /// </summary>
    /// <remarks>
    /// The generic method is closed inside the guard rather than before it: a model that does not
    /// satisfy the constraint — an aggregate without a constructor taking nothing, say — throws
    /// here, and that is a page's answer rather than the application's.
    /// </remarks>
    public static async Task<StoreAnswer> Invoke(
        MethodInfo operation, Type model, object service, object?[] arguments)
    {
        try
        {
            var task = (Task?)operation.MakeGenericMethod(model).Invoke(service, arguments);

            if (task is null)
            {
                return new StoreAnswer(null, "The store returned nothing at all.");
            }

            await task;

            var result = task.GetType().GetProperty(nameof(Task<object>.Result))?.GetValue(task);

            if (result is null)
            {
                return new StoreAnswer(null, "The store returned nothing at all.");
            }

            var resultType = result.GetType();

            if (Read(resultType, result, "IsNotSuccess") is true)
            {
                return new StoreAnswer(null, Describe(Read(resultType, result, "Failure")));
            }

            return new StoreAnswer(Read(resultType, result, "Value"), null);
        }
        catch (TargetInvocationException exception)
        {
            return new StoreAnswer(null, exception.InnerException?.Message ?? exception.Message);
        }
        catch (Exception exception)
        {
            return new StoreAnswer(null, exception.Message);
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

/// <summary>What a store operation answered.</summary>
/// <param name="Value">The value it succeeded with, which may itself be null when the store had nothing to hand back.</param>
/// <param name="Error">Why it did not succeed, or null when it did.</param>
internal sealed record StoreAnswer(object? Value, string? Error);
