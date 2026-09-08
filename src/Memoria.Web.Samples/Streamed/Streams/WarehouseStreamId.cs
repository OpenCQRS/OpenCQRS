using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Streamed.Streams;

/// <summary>
/// One stream per warehouse, holding what it despatched and what came back.
/// </summary>
/// <remarks>
/// A second stream rather than more of the customer's, because despatch is the warehouse's
/// business and its order is the warehouse's order: two pickers working the same shelves contend
/// with each other, not with the shopper placing an order. The same
/// <see cref="Events.OrderDespatchedEvent"/> can of course be appended to both — a stream is a
/// place to put events, not a claim to own them.
/// </remarks>
public class WarehouseStreamId(string warehouseCode) : IStreamId
{
    public string Id => $"warehouse:{warehouseCode}";
}
