using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Streamed.Events;

namespace Memoria.Web.Samples.Streamed.Aggregates;

/// <summary>
/// One review, folded out of the product's review stream.
/// </summary>
/// <remarks>
/// <para>
/// The write model for everything that happens to a single review. It shares its stream with every
/// other review of the same product and with
/// <see cref="Projections.ProductRatingSummary"/>, which reads the whole of it — the same
/// arrangement <see cref="Order"/> and <see cref="CustomerAccount"/> have over the customer's
/// stream.
/// </para>
/// <para>
/// Version 2 of the model. An earlier version held only the rating and the body; the title was
/// added later, and the type carries a version so a snapshot written by the old shape is not
/// mistaken for one of the new.
/// </para>
/// </remarks>
[AggregateType("ProductReview", 2)]
public class ProductReview : AggregateRoot
{
    public bool Exists { get; private set; }

    public string ReviewId { get; private set; } = string.Empty;

    public string ProductId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    public int Rating { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public bool Removed { get; private set; }

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(ProductReviewSubmittedEvent),
        typeof(ProductReviewEditedEvent),
        typeof(ProductReviewRemovedEvent)
    ];

    /// <summary>
    /// Writes the review, or explains why it cannot be written.
    /// </summary>
    public string? Submit(
        string reviewId, string productId, string customerId, int rating, string title, string body)
    {
        if (Exists) return $"Review '{reviewId}' has already been written.";
        if (rating is < 1 or > 5) return "A rating has to be between one and five stars.";
        if (string.IsNullOrWhiteSpace(body)) return "A review has to say something.";

        Add(new ProductReviewSubmittedEvent(
            reviewId, productId, customerId, rating, title, body, DateTimeOffset.UtcNow));

        return null;
    }

    /// <summary>
    /// Revises the review, or explains why it cannot be revised.
    /// </summary>
    public string? Revise(int rating, string title, string body)
    {
        if (!Exists) return "That review has not been written.";
        if (Removed) return "That review has been taken down.";
        if (rating is < 1 or > 5) return "A rating has to be between one and five stars.";

        Add(new ProductReviewEditedEvent(ReviewId, ProductId, rating, title, body, DateTimeOffset.UtcNow));

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
                Title = submitted.Title;
                Body = submitted.Body;
                return true;

            case ProductReviewEditedEvent edited:
                Rating = edited.Rating;
                Title = edited.Title;
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
/// Identifies one review inside the stream that holds all of the product's.
/// </summary>
/// <remarks>
/// The <see cref="EventPropertyFilter"/> is what lets several reviews share one stream: it narrows
/// the fold to the events carrying this review's <c>ReviewId</c>, exactly as <see cref="OrderId"/>
/// narrows the customer's stream to one order.
/// </remarks>
public class ProductReviewId(string reviewId) : IAggregateId<ProductReview>
{
    public string Id { get; } = reviewId;

    public IDictionary<string, string>? EventPropertyFilter { get; } =
        new Dictionary<string, string> { ["ReviewId"] = reviewId };
}
