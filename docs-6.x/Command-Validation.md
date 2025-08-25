# Command Validation

OpenCQRS can automatically call a validation service before a command is sent to the command handler. In order to do that a validation provider needs to be installed and configured as described [here](Configuration#validation). You can configure to validate all commands by setting the option _ValidateCommands_ to _true_ when registering the main package as described [here](Configuration#main) or on a case by case basis by setting the _ValidateCommand_ property to _true_ at the command level:

```C#
public class CreateProduct : DomainCommand<Product>
{
    public CreateProduct()
    {
        ValidateCommand = true;
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
}
```

The only validation provider currenlty available is the _FluentValidation_ provider. Therefore, you need to create a class that inherits from the **AbstractValidator<>** class of _FluentValidation_:

```C#
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
```

OpenCQRS will automatically resolve and execute it. To know more about FluentValidation please visit their [website](https://fluentvalidation.net).

Note that the assemblies that contain the validators need to be registered the same way command, query and event handlers are as explained [here](Configuration#main).

## Related

- [Commands](Commands)
- [Command Sequence](Command-Sequence)
- [Domain Commands With Event Sourcing](With-Event-Sourcing)
- [Domain Commands Without Event Sourcing](Without-Event-Sourcing)
