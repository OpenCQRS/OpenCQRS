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
    /// Asks which store the data is for.
    /// </summary>
    /// <remarks>
    /// Only asked when something is being added. A deletion on its own clears both stores, because
    /// there is nothing to choose between when the answer is "none of it".
    /// </remarks>
    public static SampleDataScope AskForScope() =>
        Ask("Which data?",
            [
                ("Streamed data", SampleDataScope.Streamed),
                ("DCB data", SampleDataScope.Dcb),
                ("Both", SampleDataScope.Both)
            ],
            SampleDataScope.None);

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
