# Memoria Web: configuration

Everything [Memoria Web](memoria-web.md) needs is configuration, and only one setting is required:
the store to open. The rest have defaults that are right for a store installed under Memoria's own
default names.

Settings are read the way ASP.NET Core reads any of them — `appsettings.json`,
`appsettings.{Environment}.json`, environment variables, then command-line arguments — so a setting
can be overridden without editing a file.

## Every setting

| Setting                          | Required                        | Default                               | What it is                                       |
| -------------------------------- | ------------------------------- | ------------------------------------- | ------------------------------------------------ |
| `ConnectionStrings:Memoria`      | Yes                             | —                                     | The store to open                                |
| `Database:Provider`              | Only when the string is unclear | Read off the connection string        | `Npgsql`, `SqlServer`, `Sqlite` or `Cosmos`      |
| `Database:Cosmos:DatabaseName`   | No                              | `Memoria`                             | Cosmos only: the database the container is in    |
| `Database:Cosmos:ContainerName`  | No                              | `Domain`                              | Cosmos only: the container the store writes into |
| `Extensions:Directory`           | No                              | `<content root>/App_Data/extensions`  | Where uploaded archives and assemblies are kept  |

As environment variables, replace each `:` with a double underscore:
`ConnectionStrings__Memoria`, `Database__Provider`, `Extensions__Directory`.

## The connection string

```json
{
  "ConnectionStrings": {
    "Memoria": "Host=localhost;Port=5432;Database=memoria_samples;Username=postgres;Password=password"
  }
}
```

The application will not start without it:

> Connection string 'Memoria' is not configured in appsettings.json.

The tool opens a store somebody else created. It creates nothing — no database, no container, no
table — so the store has to exist and carry the 1.9.0 schema already. See
[Install the store schema](../guides/install-the-store-schema.md).

### Which engine it is

The engine is read off the connection string. Most strings say plainly which one they are for,
because each provider takes keywords the others do not:

| Engine     | Recognised by                                                                            | `Database:Provider` |
| ---------- | ---------------------------------------------------------------------------------------- | ------------------- |
| PostgreSQL | `Host=`, `Port=`, `Username=`, `SslMode=`, …                                             | `Npgsql`            |
| SQL Server | `Initial Catalog=`, `Trusted_Connection=`, `(localdb)`, `tcp:`, `.database.windows.net`   | `SqlServer`         |
| SQLite     | `Data Source=` naming a `.db`/`.sqlite` file, `Mode=`, `Cache=`                           | `Sqlite`            |
| Cosmos DB  | `AccountEndpoint=`, `AccountKey=`                                                        | `Cosmos`            |

Keywords all of them take — `Database`, `Server`, `User Id`, `Password` — settle nothing and are
ignored for this purpose.

Set `Database:Provider` when the string carries signals for more than one engine, or for none. The
tool refuses to guess in either case, and says which it met:

> The provider for connection string 'Memoria' could not be read off it: it carries keywords for
> more than one provider. Set Database:Provider to Npgsql, SqlServer, Sqlite or Cosmos.

The setting is not checked against the string. It is the way out of a string the tool cannot read, so
second-guessing it would close the door it opens. The names are matched case-insensitively and
without spaces, hyphens or underscores, so `SQL Server`, `sql_server` and `sqlserver` are one answer;
`postgres`, `postgresql` and `npgsql` are another; `cosmos`, `cosmosdb` and `azurecosmosdb` a third.

Which provider was chosen is logged at start-up, because a store that answers nothing is the first
thing anyone suspects the connection string of:

```
info: Memoria.Web[0]  Store opened with Npgsql.
```

### In-memory SQLite is refused

```
Data Source=:memory:
```

is rejected at start-up rather than opening an empty store. Such a database lives only as long as the
connection that opened it, and the tool opens a connection per unit of work, so it would find nothing
whatever was seeded. Point it at a file.

## Cosmos DB

A Cosmos connection string names an account and nothing more, so where the documents are is asked for
separately:

```json
{
  "ConnectionStrings": {
    "Memoria": "AccountEndpoint=https://localhost:8081/;AccountKey=<key>"
  },
  "Database": {
    "Cosmos": {
      "DatabaseName": "memoria_samples",
      "ContainerName": "Domain"
    }
  }
}
```

Both default to what `CosmosOptions` itself defaults to — `Memoria` and `Domain` — so an account
installed under those names needs neither setting. Set them to the same values the application that
wrote the store uses, or the tool opens a container nothing has written to.

The client is built in `Gateway` connection mode. The tool asks most of its questions across
partitions, and gateway mode is the one that works from wherever an operator happens to be running
it, including from behind a corporate proxy.

A Cosmos store carries the streamed model only. The DCB menu is not shown and its addresses answer
404 — see [what each store answers](memoria-web.md#what-each-store-answers).

## Extensions

Uploaded archives and the assemblies taken out of them are kept on disk:

```
<Extensions:Directory>/
  zips/    the archives, exactly as uploaded
  lib/     the assemblies, one per name, loaded at start-up
```

The default is `App_Data/extensions` under the content root. Point `Extensions:Directory` somewhere
else to give an instance its own uploads — a scratch directory for a second instance, or a mounted
volume so a container keeps what was uploaded across restarts:

```bash
Extensions__Directory=/var/lib/memoria-web/extensions dotnet Memoria.Web.dll
```

Two behaviours worth knowing:

- **Assemblies are stored by file name alone.** An entry at `bin/Release/Contoso.dll` and one at
  `../../Contoso.dll` both land on `lib/Contoso.dll`, which is what stops a crafted archive writing
  outside the store. An assembly of the same name from an earlier upload is replaced.
- **Removing an archive re-extracts every other one.** Which assembly came from which archive is not
  recorded, so `lib/` is emptied and the archives that remain are unpacked into it again. That keeps
  two archives carrying the same assembly correct — and it means deleting one archive can bring back
  an assembly you had removed by hand.

Everything in `lib/` is loaded at start-up, so what was uploaded survives a restart as long as the
directory does.

### What to put in a zip

The assembly holding your domain types, plus any dependency of its own that the tool does not already
carry — a validation library, say. The loader resolves those from `lib/`.

**Never include a `Memoria*` assembly.** Uploaded types must bind to the ones the process already
loaded, or nothing they declare satisfies `IEvent` or `IAggregateRoot`. An assembly compiled against
a different Memoria version loads and then fails to yield types at all; the Settings page reports it:

> Contoso.Domain.dll: Could not load file or assembly 'Memoria, Version=…'

Rebuild against the version the tool was built from and upload again.

## Logging and hosting

Standard ASP.NET Core settings apply. The defaults in `appsettings.json` are:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Two loggers of the tool's own are worth raising or quieting by name:
`Memoria.Web.Settings` (uploads, removals and refreshes) and `Memoria.Web.Streamed` /
`Memoria.Web.Dcb` (snapshot refreshes).

Addresses come from `ASPNETCORE_URLS`, or from the launch profile in development — see
[Deployment](memoria-web-deployment.md).
