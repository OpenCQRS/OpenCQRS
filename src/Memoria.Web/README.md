# Memoria.Web

A browser tool for reading a Memoria store. Point it at a database, upload a zip of your own domain
assemblies on the Settings page, and it shows you the events that were appended, the aggregates and
projections snapshotted from them, and the types both were written through.

It is not a sample and not a package: it lives here, and you build and run it yourself.

```bash
dotnet run --project src/Memoria.Web
```

That serves on `http://localhost:5159` in the Development environment. Point it at a store first —
`ConnectionStrings:Memoria` in [`appsettings.json`](appsettings.json) — or fill one with
[Memoria.Web.Samples](../Memoria.Web.Samples), which carries a sample domain in both consistency
models and writes data through it.

> **Use the launch profile.** Started with `--no-launch-profile` the environment is Production, the
> development static-asset handler looks for a bundle that only exists in publish output, and the
> scoped-CSS bundle answers 500 — the application renders unstyled. The same applies to anything run
> out of `bin/` rather than out of `dotnet publish` output.

## No authentication

Anyone who can reach `/settings` can upload a `.dll` that this process will load and execute. There
is no login and no role. Run it on localhost, or behind a proxy that authenticates **every** request
including the form posts, and grant access to the people you would give shell access on that host to.

## Configuration

| Setting                         | Required                        | Default                              |
| ------------------------------- | ------------------------------- | ------------------------------------ |
| `ConnectionStrings:Memoria`     | Yes                             | —                                    |
| `Database:Provider`             | Only when the string is unclear | Read off the connection string       |
| `Database:Cosmos:DatabaseName`  | No                              | `Memoria`                            |
| `Database:Cosmos:ContainerName` | No                              | `Domain`                             |
| `Extensions:Directory`          | No                              | `<content root>/App_Data/extensions` |

PostgreSQL, SQL Server and SQLite are read through Entity Framework Core and carry both consistency
models. Cosmos DB is read through its own SDK and carries the streamed model only — there is no
Cosmos store for dynamic consistency boundaries, so those pages are neither linked nor found.

## What it writes

One thing: **Update** on a model's detail page refreshes its stored snapshot, applying the events it
has not applied yet. Nothing is appended, nothing is deleted, and no schema is created — the store
has to exist already.

## Where things are

| Path             | What is in it                                                            |
| ---------------- | ------------------------------------------------------------------------ |
| `Components/`    | The pages: `Streamed/`, `Dcb/`, settings, and the layout around them      |
| `Data/`          | Reading the connection string, and wiring whichever store it named        |
| `Extensibility/` | Uploads, assembly loading, type scanning, and the queries the pages ask   |
| `Endpoints/`     | The handful of form posts the statically rendered pages send              |
| `App_Data/`      | Uploaded archives and the assemblies taken out of them (local state)      |

## Documentation

- [Memoria Web](https://lucabriguglia.github.io/Memoria/tools/memoria-web.html) — what it is and how to use it
- [Configuration](https://lucabriguglia.github.io/Memoria/tools/memoria-web-configuration.html)
- [Deployment](https://lucabriguglia.github.io/Memoria/tools/memoria-web-deployment.html)
- [Try it with sample data](https://lucabriguglia.github.io/Memoria/tools/memoria-web-samples.html)
