using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using OpenCqrs.Commands;
using OpenCqrs.Dependencies;

namespace OpenCqrs.Validation.FluentValidation
{
    public class FluentValidationProvider : IValidationProvider
    {
        private readonly IHandlerResolver _handlerResolver;

        public FluentValidationProvider(IHandlerResolver handlerResolver)
        {
            _handlerResolver = handlerResolver;
        }

        public async Task<ValidationResponse> ValidateAsync<TCommand>(TCommand command)
            where TCommand : ICommand
        {
            var validator = _handlerResolver.ResolveHandler<IValidator<TCommand>>();
            var validationResult = await validator.ValidateAsync(command);

            return BuildValidationResponse(validationResult);
        }

        public ValidationResponse Validate<TCommand>(TCommand command)
            where TCommand : ICommand
        {
            var validator = _handlerResolver.ResolveHandler<IValidator<TCommand>>();
            var validationResult = validator.Validate(command);

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
}
