using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using FluentValidation.Results;
using OpenCqrs.Commands;

namespace OpenCqrs.Validation.FluentValidation;

public class FluentValidationProvider(IServiceProvider serviceProvider) : IValidationProvider
{
    private static readonly ConcurrentDictionary<Type, object?> CommandValidatorWrappers = new();

    public async Task<ValidationResponse> Validate<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        var validator = serviceProvider.GetService<IValidator<TCommand>>();
        if (validator is null)
        {
            throw new Exception($"Validator for {typeof(TCommand).Name} not found.");
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        return BuildValidationResponse(validationResult);
    }

    public async Task<ValidationResponse> Validate<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();

        var validator = (CommandValidatorWrapperBase<TResponse>)CommandValidatorWrappers.GetOrAdd(commandType, _ =>
            Activator.CreateInstance(typeof(CommandValidatorWrapper<,>).MakeGenericType(commandType, typeof(TResponse))))!;

        if (validator is null)
        {
            throw new Exception($"Validator for {typeof(ICommand<TResponse>).Name} not found.");
        }

        var validationResult = await validator.Validate(command, serviceProvider, cancellationToken);

        return BuildValidationResponse(validationResult);
    }

    private static ValidationResponse BuildValidationResponse(ValidationResult validationResult)
    {
        return new ValidationResponse
        {
            Errors = validationResult.Errors.Select(failure => new ValidationError
            {
                PropertyName = failure.PropertyName,
                ErrorMessage = failure.ErrorMessage
            }).ToList()
        };
    }
}
