using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Streamed.Events;

namespace Memoria.Web.Samples.Streamed.Projections;

/// <summary>
/// How a product is received, folded from every review in its stream.
/// </summary>
/// <remarks>
/// <para>
/// The read model over <see cref="Streams.ProductReviewStreamId"/>, and the counterpart to
/// <see cref="Aggregates.ProductReview"/>: where the aggregate answers a question about one review,
/// this gathers all of them into the star rating and the count a catalogue page shows.
/// </para>
/// <para>
/// It carries no event property filter, so it reads the whole stream — the same choice
/// <see cref="CustomerOrderHistory"/> makes for the same reason: a summary that leaves reviews out
/// would be a summary of the wrong thing.
/// </para>
/// </remarks>
[ProjectionType("ProductRatingSummary")]
public class ProductRatingSummary : Projection
{
    public string ProductId { get; private set; } = string.Empty;

    public int HelpfulVotes { get; private set; }

    public int UnhelpfulVotes { get; private set; }

    /// <summary>
    /// How many reviews still stand, derived from <see cref="Ratings"/> rather than counted into a
    /// field of its own, so it cannot drift from the ratings it is meant to describe across a
    /// snapshot round trip.
    /// </summary>
    public int ReviewCount => Ratings.Count;

    /// <summary>
    /// The mean rating, rounded to one place, or zero when nobody has reviewed the product yet.
    /// </summary>
    public decimal AverageRating =>
        Ratings.Count == 0 ? 0 : Math.Round((decimal)Ratings.Values.Sum() / Ratings.Count, 1);

    /// <summary>
    /// The latest rating each review settled on, so an edit moves the average rather than adding to
    /// it. Keyed by review id, replaced whole on every fold so the snapshot round trips cleanly.
    /// </summary>
    public IReadOnlyDictionary<string, int> Ratings { get; private set; } =
        new Dictionary<string, int>();

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(ProductReviewSubmittedEvent),
        typeof(ProductReviewEditedEvent),
        typeof(ProductReviewVotedEvent),
        typeof(ProductReviewRemovedEvent)
    ];

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case ProductReviewSubmittedEvent submitted:
                ProductId = submitted.ProductId;
                Ratings = With(Ratings, submitted.ReviewId, submitted.Rating);
                return true;

            case ProductReviewEditedEvent edited:
                Ratings = With(Ratings, edited.ReviewId, edited.Rating);
                return true;

            case ProductReviewVotedEvent voted:
                if (voted.Helpful)
                {
                    HelpfulVotes++;
                }
                else
                {
                    UnhelpfulVotes++;
                }

                return true;

            case ProductReviewRemovedEvent removed:
                Ratings = Without(Ratings, removed.ReviewId);
                return true;

            default:
                return false;
        }
    }

    private static IReadOnlyDictionary<string, int> With(
        IReadOnlyDictionary<string, int> ratings, string reviewId, int rating)
    {
        var updated = new Dictionary<string, int>(ratings) { [reviewId] = rating };
        return updated;
    }

    private static IReadOnlyDictionary<string, int> Without(
        IReadOnlyDictionary<string, int> ratings, string reviewId)
    {
        var updated = new Dictionary<string, int>(ratings);
        updated.Remove(reviewId);
        return updated;
    }
}

/// <summary>
/// Identifies the summary, and therefore its snapshot. One per product, and so one per stream.
/// </summary>
/// <remarks>
/// No event property filter, because the question is about every review at once. Compare
/// <see cref="Aggregates.ProductReviewId"/>, which narrows the same stream to a single review.
/// </remarks>
public class ProductRatingSummaryId(string productId) : IProjectionId<ProductRatingSummary>
{
    public string Id { get; } = productId;

    public IDictionary<string, string>? EventPropertyFilter => null;
}
