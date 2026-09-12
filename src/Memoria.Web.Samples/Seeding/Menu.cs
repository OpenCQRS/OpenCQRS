namespace Memoria.Web.Samples.Seeding;

/// <summary>What a run should do to the store.</summary>
public enum SampleDataAction
{
    /// <summary>Leave what is there and write more alongside it.</summary>
    Add,

    /// <summary>Empty the store first, then write.</summary>
    ReplaceAll,

    /// <summary>Empty the store and write nothing.</summary>
    DeleteAll,

    /// <summary>Nobody chose anything, so nothing happens.</summary>
    None
}

/// <summary>Which store a run touches.</summary>
[Flags]
public enum SampleDataScope
{
    None = 0,
    Streamed = 1,
    Dcb = 2,
    Both = Streamed | Dcb
}

/// <summary>
/// The questions the run asks before it does anything.
/// </summary>
/// <remarks>
/// Reads standard input rather than the console directly, so a choice can be piped in — which is
/// how the seeding is exercised without anyone sitting at the keyboard. End of input is a decision
/// too: it means nobody is there to answer, so the run stops rather than looping on a null.
/// </remarks>
public static class Menu
{
    /// <summary>
    /// Asks what to do with the store.
    /// </summary>
    public static SampleDataAction AskForAction() =>
        Ask("What would you like to do?",
            [
                ("Add more sample data", SampleDataAction.Add),
                ("Replace existing sample data", SampleDataAction.ReplaceAll),
                ("Delete existing sample data", SampleDataAction.DeleteAll)
            ],
            SampleDataAction.None);

    /// <summary>
    /// Asks which of the store's data the run is for.
    /// </summary>
    /// <param name="holds">
    /// What the store can hold, from <see cref="ISampleStore.Holds"/>. Only these are offered: a
    /// Cosmos store has no dynamic consistency boundary in it, and offering that half would be
    /// offering something that cannot be done.
    /// </param>
    /// <remarks>
    /// Nothing is asked when the store holds one kind only — there is nothing to choose between, and
    /// a question with a single answer is a question not worth asking. <c>Both</c> is offered as
    /// whatever the store holds rather than as the flag pair, so it never means more than the store
    /// has.
    /// </remarks>
    public static SampleDataScope AskForScope(SampleDataScope holds)
    {
        var options = new List<(string Label, SampleDataScope Choice)>();

        if (holds.HasFlag(SampleDataScope.Streamed))
        {
            options.Add(("Streamed data", SampleDataScope.Streamed));
        }

        if (holds.HasFlag(SampleDataScope.Dcb))
        {
            options.Add(("DCB data", SampleDataScope.Dcb));
        }

        if (options.Count > 1)
        {
            options.Add(("Both", holds));
        }

        return options.Count switch
        {
            0 => SampleDataScope.None,
            1 => options[0].Choice,
            _ => Ask("Which data?", options, SampleDataScope.None)
        };
    }

    private static T Ask<T>(string question, IReadOnlyList<(string Label, T Choice)> options, T nothing)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine(question);

            for (var index = 0; index < options.Count; index++)
            {
                Console.WriteLine($"  {index + 1}. {options[index].Label}");
            }

            Console.Write("> ");

            var answer = Console.ReadLine();

            if (answer is null)
            {
                Console.WriteLine();
                return nothing;
            }

            if (int.TryParse(answer.Trim(), out var chosen) && chosen >= 1 && chosen <= options.Count)
            {
                return options[chosen - 1].Choice;
            }

            Console.WriteLine($"Type a number between 1 and {options.Count}.");
        }
    }
}
