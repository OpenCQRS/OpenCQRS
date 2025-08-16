# Domain Commands With Event Sourcing

By using the _SendAsync_ method, the dispatcher will automatically publish the events set in the response of the handler and save those events to the domain store (alongside aggregate and command).

First, create a domain object that inherits from **AggregateRoot**.
This is how a simple _Product_ class might look like:

```C#
public class Product : AggregateRoot
{
    public string Title { get; private set; }

    public Product()
    {            
    }

    public Product(Guid id, string title) : base(id)
    {
        if (string.IsNullOrEmpty(title))
            throw new ApplicationException("Product title is required.");

        AddAndApplyEvent(new ProductCreated
        {
            AggregateId = Id,
            Title = title
        });
    }

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            throw new ApplicationException("Product title is required.");

        AddAndApplyEvent(new ProductTitleUpdated
        {
            AggregateId = Id,
            Title = title
        });
    }

    public void Apply(ProductCreated @event)
    {
        Id = @event.AggregateId;
        Title = @event.Title;
    }

    public void Apply(ProductTitleUpdated @event)
    {
        Title = @event.Title;
    }
}
```

Note that the empty constructor is required in order to create a new object.
After every command is executed, an event is added to the pending list of events and applied to the domain object by calling the **AddAndApplyEvent** method. The **Apply** methods are also used to load the object from history when using _GetById_ method of the Repository.

Next, create a command and an event:

```C#
public class CreateProduct : DomainCommand<Product>
{
    public string Title { get; set; }
}

public class ProductCreated : DomainEvent
{
    public string Title { get; set; }
}
```

Create the first handler or a custom service as explained [here](Commands):

```C#
public class CreateProductHandler : ICommandHandlerAsync<CreateProduct>
{
    public async Task<CommandResponse> HandleAsync(CreateProduct command)
    {
        await Task.CompletedTask;

        var product = new Product(command.AggregateId, command.Title);

        return new CommandResponse
        {
            Events = product.Events
        };
    }
}
```

Send the command using the dispatcher:

```C#
var command = new CreateProduct
{
    AggregateId = Guid.NewGuid(),
    Title = "My brand new product"
};

await _dispatcher.SendAsync(command)
```

At this stage, we might want to create our read model.
It can be achieved by sending a message to a bus or by creating an event handler that will be called automatically after the execution of the command:

```C#
public class ProductCreatedHandler : IEventHandlerAsync<ProductCreated>
{
    public async Task HandleAsync(ProductCreated @event)
    {
        await Task.CompletedTask;

        var model = new ProductViewModel
        {
            Id = @event.AggregateId,
            Title = @event.Title
        };

        FakeReadDatabase.Products.Add(model);
    }
}
```

The aggregate and the first event have been saved to the event store and the product can be retrieved from history using the repository.

Now you can add new commands, events and handlers or services as explained [here](Commands):

```C#
public class UpdateProductTitle : DomainCommand<Product>
{
    public string Title { get; set; }
}

public class ProductTitleUpdated : DomainEvent
{
    public string Title { get; set; }
}

public class UpdateProductTitleHandler : ICommandHandlerAsync<UpdateProductTitle>
{
    private readonly IRepository<Product> _repository;

    public UpdateProductTitleHandlerAsync(IRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<CommandResponse> HandleAsync(UpdateProductTitle command)
    {
        var product = await _repository.GetByIdAsync(command.AggregateId);

        if (product == null)
            throw new ApplicationException("Product not found.");

        product.UpdateTitle(command.Title);

        return new CommandResponse
        {
            Events = product.Events
        };
    }
}

public class ProductTitleUpdatedHandler : IEventHandlerAsync<ProductTitleUpdated>
{
    public async Task HandleAsync(ProductTitleUpdated @event)
    {
        await Task.CompletedTask;

        var model = FakeReadDatabase.Products.Find(x => x.Id == @event.AggregateId);
        model.Title = @event.Title;
    }
}
```

As per prevoius example, use the dispatcher to update the product.

```C#
await dispatcher.SendAsync(new UpdateProductTitle
{
    AggregateId = productId,
    Title = "Updated product title"
});
```

A new event is saved and the read model is updated using the event handler.
Next time the aggregate is loaded from history using the repository, two events will be applied in order to recreate the current state.

## Related

- [Commands](Commands)
- [Command Validation](Command-Validation)
- [Command Sequence](Command-Sequence)
- [Domain Commands Without Event Sourcing](Without-Event-Sourcing)
