using FluentValidation;

namespace OpenCqrs.Validation.FluentValidation.Tests.Models.Commands.Validators;

public class DoSomethingValidator : AbstractValidator<DoSomething>
{
    public DoSomethingValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(1, 100).WithMessage("Name length must be between 1 and 100 characters.");
    }
}
