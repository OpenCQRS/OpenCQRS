using FluentValidation;

namespace OpenCqrs.Sample.NoEventSourcing.Domain.Commands.Validators
{
    public class CreateProductValidator : AbstractValidator<CreateProduct>
    {
        public CreateProductValidator()
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .Length(1, 100).WithMessage("Product name length must be between 1 and 100 characters.");

            RuleFor(c => c.Description)
                .NotEmpty().WithMessage("Product description is required.")
                .Length(1, 100).WithMessage("Product description length must be between 1 and 100 characters.");

            RuleFor(c => c.Price)
                .GreaterThan(0.0M).WithMessage("Product price must be greater than zero.");
        }
    }
}
