namespace OpenCqrs.EventSourcing.Data;

public interface IApplicableEntity
{
    DateTimeOffset AppliedDate { get; set; }
}
