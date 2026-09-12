using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Domain;
using Memoria.Web.Samples.Dcb.Events;

namespace Memoria.Web.Samples.Dcb.Aggregates;

/// <summary>
/// One purchase order, folded from the events tagged with it.
/// </summary>
/// <remarks>
/// <para>
/// A DCB write model whose boundary is a single tag, <c>purchase-order:{id}</c>, so the store can
/// build it, fold it and snapshot it — the same arrangement <see cref="StockLevel"/> has over
/// <c>product:{id}</c>. Every decision it makes depends on this one order and nothing else.
/// </para>
/// <para>
/// It stages events under both the order's tag and the supplier's, because a purchase order belongs
/// to its supplier for the whole of its life. It reads only the order's, though: what else the
/// supplier is doing is no business of a decision about this one order.
/// </para>
/// </remarks>
[AggregateType("PurchaseOrder")]
public class PurchaseOrder : DcbAggregateRoot
{
    public bool Exists { get; private set; }

    public string SupplierId { get; private set; } = string.Empty;

    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.None;

    /// <summary>
    /// How many of each product the order asked for. A property replaced whole on every fold rather
    /// than a mutable field, so the snapshot survives the round trip the store keeps it for — the
    /// same rule <see cref="StockLevel"/> follows with its counts.
    /// </summary>
    public IReadOnlyDictionary<string, int> Ordered { get; private set; } =
        new Dictionary<string, int>();

    /// <summary>How many of each product has turned up so far.</summary>
    public IReadOnlyDictionary<string, int> Received { get; private set; } =
        new Dictionary<string, int>();

    public int LineCount => Ordered.Count;

    public override Type[]? EventTypeFilter { get; } =
    [
        typeof(PurchaseOrderRaisedEvent),
        typeof(PurchaseOrderLineAddedEvent),
        typeof(PurchaseOrderApprovedEvent),
        typeof(PurchaseOrderReceivedEvent),
        typeof(PurchaseOrderCancelledEvent)
    ];

    /// <summary>
    /// The order this fold is about, taken from the boundary rather than from a constructor.
    /// </summary>
    private string PurchaseOrderCode => Tags.Single(tag => tag.Key == "purchase-order").Value;

    /// <summary>
    /// Opens the order against a supplier, or explains why it cannot be opened.
    /// </summary>
    public string? Raise(string purchaseOrderId, string supplierId)
    {
        if (Exists) return $"Purchase order '{purchaseOrderId}' has already been raised.";
        if (string.IsNullOrWhiteSpace(supplierId)) return "A purchase order needs a supplier.";

        Add(new PurchaseOrderRaisedEvent(purchaseOrderId, supplierId, DateTimeOffset.UtcNow),
            new Tag("purchase-order", purchaseOrderId), new Tag("supplier", supplierId));

        return null;
    }

    /// <summary>
    /// Adds a line to the order, or explains why it cannot be added.
    /// </summary>
    public string? AddLine(string productId, int quantity, decimal unitCost)
    {
        if (!Exists) return "That purchase order has not been raised.";
        if (Status != PurchaseOrderStatus.Draft) return $"An order that is {Describe(Status)} can no longer be edited.";
        if (quantity < 1) return $"A line has to be at least one unit, not {quantity}.";
        if (unitCost < 0) return "A cost cannot be negative.";
        if (Ordered.ContainsKey(productId)) return $"'{productId}' is already on this order.";

        Add(new PurchaseOrderLineAddedEvent(PurchaseOrderCode, productId, quantity, unitCost),
            new Tag("purchase-order", PurchaseOrderCode), new Tag("supplier", SupplierId));

        return null;
    }

    /// <summary>
    /// Approves the order and sends it, or explains why it cannot be approved.
    /// </summary>
    public string? Approve(string approvedBy)
    {
        if (!Exists) return "That purchase order has not been raised.";
        if (Status != PurchaseOrderStatus.Draft) return $"That order is already {Describe(Status)}.";
        if (Ordered.Count == 0) return "An empty purchase order cannot be approved.";

        Add(new PurchaseOrderApprovedEvent(PurchaseOrderCode, approvedBy, DateTimeOffset.UtcNow),
            new Tag("purchase-order", PurchaseOrderCode), new Tag("supplier", SupplierId));

        return null;
    }

    /// <summary>
    /// Records goods arriving against the order, or explains why they cannot have.
    /// </summary>
    public string? Receive(string productId, int quantity)
    {
        if (Status != PurchaseOrderStatus.Approved) return $"An order that is {Describe(Status)} is not expecting deliveries.";
        if (quantity < 1) return $"A delivery has to bring at least one unit, not {quantity}.";

        var ordered = Ordered.GetValueOrDefault(productId);
        if (ordered == 0) return $"'{productId}' is not on this order.";

        var received = Received.GetValueOrDefault(productId);
        if (received + quantity > ordered)
        {
            return $"Only {ordered - received} of '{productId}' is still outstanding.";
        }

        Add(new PurchaseOrderReceivedEvent(PurchaseOrderCode, productId, quantity),
            new Tag("purchase-order", PurchaseOrderCode), new Tag("supplier", SupplierId));

        return null;
    }

    /// <summary>
    /// Cancels the order, or explains why it is too late.
    /// </summary>
    public string? Cancel(string reason)
    {
        if (!Exists) return "That purchase order has not been raised.";
        if (Status == PurchaseOrderStatus.Cancelled) return "That order is already cancelled.";
        if (Status == PurchaseOrderStatus.Received) return "That order has already been received in full.";

        Add(new PurchaseOrderCancelledEvent(PurchaseOrderCode, reason),
            new Tag("purchase-order", PurchaseOrderCode), new Tag("supplier", SupplierId));

        return null;
    }

    protected override bool Apply<T>(T @event)
    {
        switch (@event)
        {
            case PurchaseOrderRaisedEvent raised:
                Exists = true;
                SupplierId = raised.SupplierId;
                Status = PurchaseOrderStatus.Draft;
                return true;

            case PurchaseOrderLineAddedEvent added:
                Ordered = With(Ordered, added.ProductId, added.Quantity);
                return true;

            case PurchaseOrderApprovedEvent:
                Status = PurchaseOrderStatus.Approved;
                return true;

            case PurchaseOrderReceivedEvent received:
                Received = With(Received, received.ProductId,
                    Received.GetValueOrDefault(received.ProductId) + received.Quantity);
                if (FullyReceived()) Status = PurchaseOrderStatus.Received;
                return true;

            case PurchaseOrderCancelledEvent:
                Status = PurchaseOrderStatus.Cancelled;
                return true;

            default:
                return false;
        }
    }

    private bool FullyReceived() =>
        Ordered.Count > 0 && Ordered.All(line => Received.GetValueOrDefault(line.Key) >= line.Value);

    private static IReadOnlyDictionary<string, int> With(
        IReadOnlyDictionary<string, int> lines, string productId, int quantity)
    {
        var updated = new Dictionary<string, int>(lines) { [productId] = quantity };
        return updated;
    }

    private static string Describe(PurchaseOrderStatus status) =>
        status == PurchaseOrderStatus.None ? "not yet raised" : status.ToString().ToLowerInvariant();
}

/// <summary>
/// Where a purchase order has got to.
/// </summary>
public enum PurchaseOrderStatus
{
    /// <summary>Nothing has happened yet — the fold found no events.</summary>
    None = 0,
    Draft = 1,
    Approved = 2,
    Received = 3,
    Cancelled = 4
}

/// <summary>
/// Identifies one purchase order: one tag, holding everything ever recorded about it.
/// </summary>
public class PurchaseOrderId(string purchaseOrderId) : IDcbAggregateId<PurchaseOrder>
{
    public string Id { get; } = purchaseOrderId;

    public TagQuery Boundary { get; } = TagQuery.AnyOf(new Tag("purchase-order", purchaseOrderId));
}
