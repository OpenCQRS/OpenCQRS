﻿using FluentValidation;

namespace OpenCqrs.Validation.FluentValidation.Tests.Models.Commands.Validators;

public class FirstCommandInSequenceValidator : AbstractValidator<FirstCommandInSequence>
{
    public FirstCommandInSequenceValidator()
    {
        RuleFor(c => c.Name).NotEmpty().WithMessage("Name is required.");
    }
}
