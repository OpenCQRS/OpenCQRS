namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// The words the sample data is made of, and the small random helpers that pick between them.
/// </summary>
/// <remarks>
/// Identifiers are readable and short on purpose. Someone reading a run's report has to type them
/// back into the web tool by hand, and a GUID is a poor thing to copy off a terminal. The four-digit
/// suffix is there so a second run adds to the store rather than colliding with the first.
/// </remarks>
public static class SampleVocabulary
{
    private static readonly string[] Products =
    [
        "Espresso Machine", "Walnut Desk", "Linen Apron", "Cast Iron Pan", "Reading Lamp",
        "Wool Blanket", "Chef's Knife", "Canvas Holdall", "Ceramic Mug", "Bamboo Chopping Board",
        "Leather Notebook", "Enamel Kettle"
    ];

    private static readonly string[] Carriers = ["Royal Mail", "DPD", "Evri", "DHL"];

    private static readonly string[] Warehouses = ["LDN-1", "MAN-2", "GLA-3"];

    private static readonly string[] CancellationReasons =
    [
        "changed their mind", "found it cheaper elsewhere", "ordered the wrong size",
        "no longer needed"
    ];

    private static readonly string[] DiscontinuationReasons =
    [
        "supplier stopped making it", "replaced by the new model", "poor margin"
    ];

    private static readonly string[] AdjustmentReasons =
    [
        "stock count", "damaged in the warehouse", "found behind the racking"
    ];

    public static string ProductName(Random random) => Pick(random, Products);

    public static string Carrier(Random random) => Pick(random, Carriers);

    public static string Warehouse(Random random) => Pick(random, Warehouses);

    public static string CancellationReason(Random random) => Pick(random, CancellationReasons);

    public static string DiscontinuationReason(Random random) => Pick(random, DiscontinuationReasons);

    public static string AdjustmentReason(Random random) => Pick(random, AdjustmentReasons);

    /// <summary>
    /// An identifier of the given kind, short enough to retype.
    /// </summary>
    public static string Id(Random random, string prefix) => $"{prefix}-{Token(random)}";

    /// <summary>
    /// A stock keeping unit, made from the product's name so the two are recognisably the same
    /// thing in a listing.
    /// </summary>
    public static string Sku(Random random, string productName)
    {
        var letters = new string(productName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]))
            .ToArray());

        return $"{letters}-{Token(random)}";
    }

    /// <summary>
    /// A price with two decimal places, which is what a price has.
    /// </summary>
    public static decimal Price(Random random, int lowest, int highest) =>
        Math.Round(random.Next(lowest * 100, highest * 100) / 100m, 2);

    /// <summary>
    /// A moment in the last few weeks, so a listing sorted by date is not all one instant.
    /// </summary>
    public static DateTimeOffset RecentMoment(Random random, TimeProvider time) =>
        time.GetUtcNow()
            .AddDays(-random.Next(1, 30))
            .AddMinutes(-random.Next(0, 1440));

    /// <summary>
    /// True with the given chance, written as a percentage so the call sites read as odds.
    /// </summary>
    public static bool Chance(Random random, int percent) => random.Next(100) < percent;

    private static string Token(Random random) => random.Next(0x1000, 0x10000).ToString("x4");

    private static string Pick(Random random, string[] from) => from[random.Next(from.Length)];
}
