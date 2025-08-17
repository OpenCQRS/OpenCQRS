namespace OpenCqrs.Data;

public interface IAuditableEntity
{
    DateTimeOffset CreatedDate { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset LatestUpdatedDate { get; set; }
    string? LatestUpdatedBy { get; set; }
}
