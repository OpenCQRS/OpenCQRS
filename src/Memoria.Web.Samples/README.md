# Memoria.Web.Samples

Fills a Memoria store with sample domain data, and carries the sample domain types
that data is written through. Run it against a store, upload its assembly to
[Memoria.Web](../Memoria.Web), and the tool has something to show.

Two jobs in one project on purpose: the types the tool displays are the types the
seeder exercises, so nothing reaches the tool that was never written through the
framework first.

```bash
dotnet run --project src/Memoria.Web.Samples
```

```
Memoria.Web.Samples
  store    : Npgsql
  writing  : memoria_samples
  bound    : 28 events, 4+4 aggregates, 3+5 projections (streamed+dcb)
  schema   : installed
```

The default store is PostgreSQL on `localhost:5432`, database `memoria_samples` —
see [Point it at a store](#point-it-at-a-store) for the others.

## What it asks

Two questions, answered by number on standard input. End of input is an answer too:
it means nobody is there to answer, so the run stops rather than looping.

| Answer | Action                                               |
| ------ | ---------------------------------------------------- |
| `1`    | Add — writes alongside whatever is already there     |
| `2`    | Replace — empties the store, then writes             |
| `3`    | Delete — empties the store and writes nothing        |
| (none) | Nothing chosen; the run stops without touching a row |

Then, **only when the store holds both models**, which data:

| Answer | Scope         |
| ------ | ------------- |
| `1`    | Streamed data |
| `2`    | DCB data      |
| `3`    | Both          |

A Cosmos store holds the streamed model only, so this question is not asked there —
see [Limits](#limits).

| Exit code | Meaning                                                    |
| --------- | ---------------------------------------------------------- |
| `0`       | The run finished, including a run where nothing was chosen |
| `1`       | The store could not be reached; nothing was written        |

## What it writes

**Streamed** — customers, the orders they place, and the reviews products collect.
Every order is driven through the `Order` aggregate's own methods, so the log holds
only sequences the domain would have allowed. Some reviews go through
`ProductReviewV1` rather than `ProductReview`, so the store ends up holding snapshots
of both shapes side by side.

**DCB** — a catalogue, stock for it, orders holding some of that stock, and the
purchase orders that restock it. Every append follows the read-decide-append cycle on
condition that the boundary has not moved, the way an application would write it.

Snapshots are deliberately left in three states, because a store where everything is
current has nothing to demonstrate:

| State       | How it is reached                                              |
| ----------- | -------------------------------------------------------------- |
| Up to date  | Snapshotted after the last event on its stream                 |
| Behind      | Snapshotted, then more events appended without snapshotting again |
| No snapshot | Events appended and never snapshotted                          |

The run ends with every model it wrote, the values its identifier was built from, and
where its snapshot stands against its stream — so you know which rows have an update
waiting for them in the tool:

```
store    kind       model                 identifier            values     snapshot
streamed aggregate  Order                 OrderId               o-5b0f     v4, up to date
streamed aggregate  CustomerAccount       CustomerAccountId     c-37e7     v2 of 6 — 4 behind
streamed projection OrderSummary          OrderSummaryId        o-a6f6     no snapshot — 2 events waiting
```

## Point it at a store

The connection string is `ConnectionStrings:Memoria` in
[`appsettings.json`](appsettings.json). The engine is read off the string itself; set
`Database:Provider` only when the string could be more than one of them, or when it
names none.

| Engine     | Recognized by                                                                          | `Database:Provider` |
| ---------- | -------------------------------------------------------------------------------------- | ------------------- |
| PostgreSQL | `Host=`, `Port=`, `Username=`, `SslMode=`, …                                           | `Npgsql`            |
| SQL Server | `Initial Catalog=`, `Trusted_Connection=`, `(localdb)`, `tcp:`, `.database.windows.net` | `SqlServer`         |
| SQLite     | `Data Source=` naming a `.db`/`.sqlite` file, `Mode=`, `Cache=`                        | `Sqlite`            |
| Cosmos DB  | `AccountEndpoint=`, `AccountKey=`                                                      | `Cosmos`            |

This is the same resolution the web tool uses —
[`DatabaseConnection`](../Memoria.Web/Data/DatabaseConnection.cs) is compiled into both
— so a string that reaches one reaches the other.

The seeder creates what is missing before it writes: the database and each store's
tables on a relational engine, the database and the container on Cosmos.

### Cosmos DB

A Cosmos connection string names an account and nothing more, so the database and the
container are separate settings. They default to what the store's own options default
to, so an account installed under those names needs no settings at all.

| Setting                         | Default   |
| ------------------------------- | --------- |
| `Database:Cosmos:DatabaseName`  | `Memoria` |
| `Database:Cosmos:ContainerName` | `Domain`  |

Set them to the same values [Memoria.Web](../Memoria.Web) is given, or the seeder
fills a container the tool does not open.

Against the local emulator, start it before the run — otherwise the run stops with
"Could not reach the database" before asking anything.

### Running against another store without editing settings

Every setting can be overridden on the command line, which is how to point a run at a
scratch store rather than the one in `appsettings.json`:

```bash
echo "1" | dotnet run --project src/Memoria.Web.Samples -- \
  "--ConnectionStrings:Memoria=AccountEndpoint=https://localhost:8081/;AccountKey=<key>" \
  "--Database:Cosmos:DatabaseName=MemoriaScratch"
```

## Load the types into Memoria.Web

The tool reads uploaded assemblies and has no reference to this project. Build it, zip
the assembly on its own, and upload the zip on the tool's Settings page.

```bash
dotnet build src/Memoria.Web.Samples --configuration Release
```

```powershell
Compress-Archive -Path src\Memoria.Web.Samples\bin\Release\net10.0\Memoria.Web.Samples.dll -DestinationPath Memoria.Web.Samples.zip -Force
```

The archive holds `Memoria.Web.Samples.dll` and nothing else. Never put a `Memoria*`
core assembly in it: uploaded types have to bind to the ones the tool already loaded.

The assembly is compiled against the `<Version>` in
[`Directory.Build.props`](../../Directory.Build.props), so a version change means a
rebuild and a re-upload — one built against the old version still loads, and then
contributes no types at all.

## Limits

**Cosmos holds the streamed model only.** There is no Cosmos dynamic consistency
boundary store, and the model would not build on that provider if there were. A Cosmos
run is never offered the DCB half, rather than being offered it and refused on the
first write.

**In-memory SQLite is refused.** A `Data Source=:memory:` database lives only as long
as the connection that opened it, and both this and the tool open a connection per
unit of work — they would find an empty store rather than the one that was seeded.
Point it at a file.

**Do not upload this alongside `Memoria.Examples.Ecommerce.Dcb`.** Both claim the
event types `ProductCreated`, `ProductDeleted` and `ProductDetailsChanged` at version
1, and the DCB aggregate `Product` at version 1. Whichever loses the name loses its
bindings, and its pages then report no events inside the boundary.

## Where things are

| Path        | What is in it                                                           |
| ----------- | ----------------------------------------------------------------------- |
| `Streamed/` | The streamed model: streams, aggregates, projections, events            |
| `Dcb/`      | The dynamic consistency boundary model: aggregates, projections, events |
| `Seeding/`  | The run itself — the menu, the store it writes to, and the data         |
| `Data/`     | The two contexts a relational store is written through                  |
