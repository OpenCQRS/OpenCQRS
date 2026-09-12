# Memoria Web: deployment

[Memoria Web](memoria-web.md) is not published to NuGet. It is an ASP.NET Core application in the
repository, and you build it, publish it, and host it yourself.

> **Read [Security](memoria-web.md#security) before deciding where to put it.** There is no
> authentication or authorization of any kind in this release, and anyone who can reach `/settings`
> can upload an assembly this process will load and execute. Both are coming in the next release;
> everything on this page describes the tool as it stands.

## Run it locally

```bash
git clone https://github.com/lucabriguglia/Memoria.git
cd Memoria
dotnet run --project src/Memoria.Web
```

That uses the project's launch profile: the Development environment, on `http://localhost:5159`
(`https://localhost:7197` under the `https` profile). Point it at your store first — see
[Configuration](memoria-web-configuration.md) — or at the sample one, which
[Try it with sample data](memoria-web-samples.md) fills for you.

> **Use the launch profile, or set `ASPNETCORE_ENVIRONMENT` yourself.** Started with
> `--no-launch-profile`, the environment is Production and the development static-asset handler
> looks for a bundle that only exists in publish output. The scoped-CSS bundle then answers 500 and
> the application renders unstyled. The same applies to anything run out of `bin/` rather than out of
> `dotnet publish` output.

## Publish it

```bash
dotnet publish src/Memoria.Web --configuration Release --output ./web
```

Run the result with the ASP.NET Core 10 runtime:

```bash
cd web
ASPNETCORE_URLS=http://localhost:5000 \
ConnectionStrings__Memoria="Host=db;Port=5432;Database=memoria;Username=reader;Password=…" \
dotnet Memoria.Web.dll
```

Publish output serves its static assets correctly in any environment. Build output does not — see the
note above.

## What the host has to provide

| Requirement            | Why                                                                                     |
| ---------------------- | --------------------------------------------------------------------------------------- |
| ASP.NET Core 10 runtime | The published application is framework-dependent                                        |
| Network to the store    | The only external dependency there is                                                   |
| A writable content root | Uploaded archives and assemblies are written under it unless `Extensions:Directory` moves them elsewhere |

Nothing else. There is no cache, no message broker, no background worker and no scheduled job.

Every page renders statically — all of their state travels in the query string — so no component
declares an interactive render mode and no Blazor circuit is opened. Ordinary HTTP proxying is
enough; nothing here needs a WebSocket today.

### HTTPS

The pipeline calls `UseHttpsRedirection` always, and `UseHsts` outside Development. Terminating TLS at
a reverse proxy is the usual arrangement; configure
[forwarded headers](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer) on the
proxy side so the application sees the original scheme, or the redirect will fight the proxy.

### Keeping uploads across restarts

Everything in the extensions directory is read again at start-up, so uploads survive a restart as
long as that directory does. In a container, mount it:

```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e ConnectionStrings__Memoria="Host=db;Port=5432;Database=memoria;Username=reader;Password=…" \
  -e Extensions__Directory=/data/extensions \
  -v memoria-web-extensions:/data/extensions \
  memoria-web:latest
```

Without a volume, every deployment starts with nothing installed and everyone has to upload again.

### Run one instance

Type registration lives in the process. Two instances behind a load balancer each register their own
uploads from their own extensions directory, so the request after an upload can land on an instance
that has never seen the assembly. Run one instance unless you have a reason not to; if you must run
more, share the extensions directory between them and accept that an instance only picks up another's
upload when it is restarted or someone refreshes the types on it.

There is no horizontal-scale case to make here. The tool is read-mostly, its queries are the store's,
and the load it puts on a host is one operator at a time.

### A Dockerfile

None ships with the repository. This is the standard shape, if you want one:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Memoria.Web -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Memoria.Web.dll"]
```

## Putting authentication in front of it

The application has none in this release, so the proxy has to be the whole of it. Whatever you use —
an identity-aware proxy, OAuth2 Proxy, a Kubernetes ingress with an auth annotation, basic auth on
nginx — the requirement is the same: **no request reaches the application unauthenticated**,
including `POST /settings/upload`. Protecting the pages and leaving the form posts open protects
nothing.

Grant access to the people you would give shell access on that host to, because an uploaded assembly
runs with the application's own privileges.

The next release adds authentication and authorization to the application itself, which will make
this section a choice rather than the only option. A proxy in front of it stays perfectly valid
either way — and until then it is the only thing standing between the internet and an upload form
that runs code.

## Pointing it at production data

Perfectly reasonable, with two precautions:

- **Use a read-only account** unless operators are expected to refresh snapshots. Reading needs
  `SELECT` on the store's tables (or read access to the Cosmos container); **Update** additionally
  needs to write the aggregate and projection rows.
- **Expect the queries to be the store's queries.** Data pages read the same tables the application
  does. They page rather than fetching whole streams, and against a relational store the list queries
  are run once in the background at start-up so nobody's first page load pays to build the model and
  compile them — but a wide filter over a large store is still a query against your production
  database.

The tool creates nothing and deletes nothing. The only write it can make is refreshing a snapshot —
see [the one thing it writes](memoria-web.md#the-one-thing-it-writes).
