using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using FluentValidation.Results;
using OpenCqrs.Commands;

namespace OpenCqrs.Validation.FluentValidation;

public class FluentValidationProvider(IServiceProvider serviceProvider) : IValidationProvider
{
    public async Task<ValidationResponse> Validate<TCommand>(TCommand command) where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        var validator = serviceProvider.GetService<IValidator<TCommand>>();
        if (validator is null)
        {
            throw new Exception($"Validator for {typeof(TCommand).Name} not found.");
        }
        
        var validationResult = await validator.ValidateAsync(command);
        
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
