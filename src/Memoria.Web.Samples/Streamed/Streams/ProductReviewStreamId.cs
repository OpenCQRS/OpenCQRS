using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Streamed.Streams;

/// <summary>
/// One stream per product, holding every review ever written about it.
/// </summary>
/// <remarks>
/// <para>
/// A third stream alongside the customer's and the warehouse's, and it makes the same choice they
/// do about what a version number counts: here it counts everything said about one product, so two
/// reviews of the same line can never be written at the same instant, while every question about
/// how the product is received is answered by one ordered read.
/// </para>
/// <para>
/// Several models fold from it. <see cref="Aggregates.ProductReview"/> narrows it to a single review
/// by event property, <see cref="Projections.ProductRatingSummary"/> reads the lot to work out an
/// average. The stream does not know or care which.
/// </para>
/// </remarks>
public class ProductReviewStreamId(string productId) : IStreamId
{
    public string Id => $"product-reviews:{productId}";
}
