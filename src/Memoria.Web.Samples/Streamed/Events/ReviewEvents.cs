using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Streamed.Events;

/// <summary>
/// A customer wrote a review of a product.
/// </summary>
/// <remarks>
/// Every event in this file carries <c>ReviewId</c> as its first value, for the same reason the
/// order events carry <c>OrderId</c>: the product's review stream holds all of its reviews, so a
/// model about one review narrows the stream with <c>EventPropertyFilter["ReviewId"]</c> — which
/// can only find a property that is there. See <see cref="Aggregates.ProductReviewId"/>.
/// </remarks>
[EventType("ProductReviewSubmitted")]
public record ProductReviewSubmittedEvent(
    string ReviewId,
    string ProductId,
    string CustomerId,
    int Rating,
    string Title,
    string Body,
    DateTimeOffset SubmittedOn) : IEvent;

/// <summary>
/// The author revised a review they had already written.
/// </summary>
/// <remarks>
/// Version 2 of this event. The first version carried only the new body; a later change to the
/// sample decided that a revision may also move the rating, and rather than overload the meaning of
/// the old shape it was given a version of its own. Both would deserialize side by side in a store
/// that still holds the first — which is the whole reason a version is a stable number rather than
/// a class name.
/// </remarks>
[EventType("ProductReviewEdited", 2)]
public record ProductReviewEditedEvent(
    string ReviewId,
    string ProductId,
    int Rating,
    string Title,
    string Body,
    DateTimeOffset EditedOn) : IEvent;

/// <summary>
/// Another shopper marked a review as helpful, or not.
/// </summary>
/// <remarks>
/// A vote is a new fact rather than an edit of a running total, so the model that folds these is
/// free to count them however the page wants — helpful against unhelpful, or a net score.
/// </remarks>
[EventType("ProductReviewVoted")]
public record ProductReviewVotedEvent(string ReviewId, string ProductId, bool Helpful) : IEvent;

/// <summary>
/// A review was taken down, because it broke the rules or the author asked.
/// </summary>
[EventType("ProductReviewRemoved")]
public record ProductReviewRemovedEvent(string ReviewId, string ProductId, string Reason) : IEvent;
