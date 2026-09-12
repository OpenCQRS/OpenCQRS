namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// One model this run wrote, and how to find out where its snapshot stands.
/// </summary>
/// <param name="Store">Which store it lives in.</param>
/// <param name="Kind">Whether it is a write model or a read model.</param>
/// <param name="Model">The model type's name.</param>
/// <param name="Identifier">The identifier type's name, which is what the web tool asks for.</param>
/// <param name="Values">The values that identifier was built from, in constructor order.</param>
/// <param name="Measure">
/// Reads the snapshot's version and the version a full fold reaches. Deferred rather than recorded
/// at the time, because the seeding goes on appending after a model is written and the gap only
/// means anything once the run has finished.
/// </param>
public sealed record SeededModel(
    string Store,
    string Kind,
    string Model,
    string Identifier,
    string Values,
    Func<Task<(int Snapshot, int Folded)>> Measure);

/// <summary>
/// What a run wrote, gathered so it can be shown at the end.
/// </summary>
public sealed class SeedReport
{
    private readonly List<SeededModel> _models = [];

    public IReadOnlyList<SeededModel> Models => _models;

    public int Events { get; private set; }

    public void Add(SeededModel model) => _models.Add(model);

    public void Appended(int events) => Events += events;

    /// <summary>
    /// Prints every model with the gap between its snapshot and its stream, so a run says plainly
    /// which rows have an update waiting for them in the web tool.
    /// </summary>
    /// <remarks>
    /// The gap is measured rather than assumed. The seeding chooses when to refresh a snapshot and
    /// when to leave it, but what the store actually holds is the only answer worth printing.
    /// </remarks>
    public async Task Print()
    {
        if (_models.Count == 0)
        {
            Console.WriteLine("Nothing was written.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"{Events} events appended, {_models.Count} models written.");
        Console.WriteLine();
        Console.WriteLine($"{"store",-9}{"kind",-11}{"model",-26}{"identifier",-28}{"values",-24}snapshot");
        Console.WriteLine(new string('-', 116));

        var behind = 0;

        foreach (var model in _models)
        {
            var (snapshot, folded) = await model.Measure();

            var standing = snapshot switch
            {
                0 when folded == 0 => "no snapshot, nothing to fold",
                0 => $"no snapshot — {folded} {(folded == 1 ? "event" : "events")} waiting",
                _ when snapshot < folded => $"v{snapshot} of {folded} — {folded - snapshot} behind",
                _ => $"v{snapshot}, up to date"
            };

            if (snapshot < folded)
            {
                behind++;
            }

            Console.WriteLine(
                $"{model.Store,-9}{model.Kind,-11}{model.Model,-26}{model.Identifier,-28}{model.Values,-24}{standing}");
        }

        Console.WriteLine();
        Console.WriteLine($"{behind} of {_models.Count} are behind their events. Open one in Memoria.Web,");
        Console.WriteLine("type the values above into its identifier, and update it to fold the rest in.");
    }
}
