namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

public interface IApplicableEntity
{
    DateTimeOffset AppliedDate { get; set; }
}
