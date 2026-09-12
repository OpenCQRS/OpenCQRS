using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Streamed.Events;

namespace Memoria.Web.Samples.Streamed.Aggregates;

/// <summary>
/// One review, folded out of the product's review stream — the shape this model was first written
/// in.
/// </summary>
/// <remarks>
/// <para>
/// Version 1 of <c>ProductReview</c>, kept beside the version 2 in <see cref="ProductReview"/> so
/// the pair reads as a model evolving rather than a name reused. A snapshot written by this shape
/// carries no title, which is the single difference from version 2: the later model added
/// <see cref="ProductReview.Title"/>, and the version is what keeps the two snapshots apart.
/// </para>
/// <para>
/// It shares its stream with every other review of the same product and with
/// <see cref="Projections.ProductRatingSummary"/>, exactly as version 2 does.
/// </para>
/// </remarks>
[AggregateType("ProductReview", 1)]
public class ProductReviewV1 : AggregateRoot
{
    public bool Exists { get; private set; }

    public string ReviewId { get; private set; } = string.Empty;

    public string ProductId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    public int Rating { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public bool Removed { get; private set; }

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(ProductReviewSubmittedEvent),
        typeof(ProductReviewEditedEventV1),
        typeof(ProductReviewRemovedEvent)
    ];

    /// <summary>
    /// Writes the review, or explains why it cannot be written.
    /// </summary>
    public string? Submit(
        string reviewId, string productId, string customerId, int rating, string body)
    {
        if (Exists) return $"Review '{reviewId}' has already been written.";
        if (rating is < 1 or > 5) return "A rating has to be between one and five stars.";
        if (string.IsNullOrWhiteSpace(body)) return "A review has to say something.";

        Add(new ProductReviewSubmittedEvent(
            reviewId, productId, customerId, rating, string.Empty, body, DateTimeOffset.UtcNow));

        return null;
    }

    /// <summary>
    /// Revises the review's words, or explains why it cannot be revised. Version 1 could change the
    /// body but not the stars.
    /// </summary>
    public string? Revise(string body)
    {
        if (!Exists) return "That review has not been written.";
        if (Removed) return "That review has been taken down.";

        Add(new ProductReviewEditedEventV1(ReviewId, ProductId, body, DateTimeOffset.UtcNow));

        return null;
    }

    /// <summary>
    /// Takes the review down, or explains why there is nothing to take down.
    /// </summary>
    public string? Remove(string reason)
    {
        if (!Exists) return "That review has not been written.";
        if (Removed) return "That review has already been taken down.";

        Add(new ProductReviewRemovedEvent(ReviewId, ProductId, reason));

        return null;
    }

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case ProductReviewSubmittedEvent submitted:
                Exists = true;
                ReviewId = submitted.ReviewId;
                ProductId = submitted.ProductId;
                CustomerId = submitted.CustomerId;
                Rating = submitted.Rating;
                Body = submitted.Body;
                return true;

            case ProductReviewEditedEventV1 edited:
                Body = edited.Body;
                return true;

            case ProductReviewRemovedEvent:
                Removed = true;
                return true;

            default:
                return false;
        }
    }
}

/// <summary>
/// Identifies one version 1 review inside the stream that holds all of the product's.
/// </summary>
public class ProductReviewV1Id(string reviewId) : IAggregateId<ProductReviewV1>
{
    public string Id { get; } = reviewId;

    public IDictionary<string, string>? EventPropertyFilter { get; } =
        new Dictionary<string, string> { ["ReviewId"] = reviewId };
}
