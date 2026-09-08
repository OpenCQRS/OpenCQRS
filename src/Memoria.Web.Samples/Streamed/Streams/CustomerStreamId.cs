using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Samples.Streamed.Streams;

/// <summary>
/// One stream per customer, holding every order they have ever placed.
/// </summary>
/// <remarks>
/// <para>
/// The choice this stream makes is the one a streamed model always has to make: what a version
/// number counts. Here it counts everything the customer did, so two of their orders can never be
/// written at the same moment — and in return every question about the customer is answered by one
/// ordered read. A stream per order would trade that away for concurrency the shopper is unlikely
/// to need.
/// </para>
/// <para>
/// Several models are folded from it. <see cref="Aggregates.Order"/> narrows it to a single order
/// by event property, <see cref="Aggregates.CustomerAccount"/> reads the lot. The stream does not
/// know or care which; see <c>docs/guides/multiple-aggregates-per-stream.md</c>.
/// </para>
/// </remarks>
public class CustomerStreamId(string customerId) : IStreamId
{
    public string Id => $"customer:{customerId}";
}
