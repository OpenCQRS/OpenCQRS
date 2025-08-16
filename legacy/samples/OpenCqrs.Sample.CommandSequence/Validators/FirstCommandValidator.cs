using FluentValidation;
using OpenCqrs.Sample.CommandSequence.Commands;

namespace OpenCqrs.Sample.CommandSequence.Validators
{
    public class FirstCommandValidator : AbstractValidator<FirstCommand>
    {
        public FirstCommandValidator()
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Name is required.")
                .Length(1, 100).WithMessage("Name length must be between 1 and 100 characters.");
        }
    }
}
