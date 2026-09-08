/*
    Memoria 1.9.0 — rename the event table to DomainEvents (SQL Server)

    Memoria 1.9.0 maps EventEntity to DomainEvents. Every earlier version mapped it to `events`, the
    one store table that did not say whose it was — and the name most likely to already be taken in a
    database Memoria shares with anything else. The store reads and writes DomainEvents from 1.9.0
    onwards, so an existing database must be renamed before the upgraded application runs against it.

    This is a metadata-only rename: no rows are copied and no index is rebuilt, so it costs the same
    on a stream of ten events as on one of ten million. It does need a schema-modification lock, so
    run it while nothing is writing.

    Assumes the default table names and the dbo schema. Adjust the identifiers below if your
    DbContext maps them elsewhere.

    Safe to run more than once: a database already holding DomainEvents and no `events` is left
    alone. A database holding BOTH is an error rather than a no-op — that is what running the 1.9.0
    install script before this one produces, and quietly doing nothing would strand every existing
    event in a table the store no longer reads.

    If you manage this database with EF Core migrations, do NOT run this. Your DbContext derives from
    DomainDbContext, so `dotnet ef migrations add` generates the rename from the model and keeps
    __EFMigrationsHistory in step. See docs/guides/upgrade-1.9.0.md.

    The three indexes keep their names — IX_Events_EventType, IX_Events_StreamId_CreatedDate and
    IX_Events_StreamId_Sequence — because they are named for the entity, not the table, and follow it
    across the rename. The primary key does not: EF names it after the table, so PK_events becomes
    PK_DomainEvents below. Leaving it would make a migrated database differ from one this version
    creates, which is exactly the drift the install-script comparison exists to catch.

    Deliberately contains no GO separators, so it runs as a single batch under any client — sqlcmd,
    SSMS, Azure Data Studio, or a plain SqlCommand.
*/

SET XACT_ABORT ON;

IF OBJECT_ID(N'[dbo].[events]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[DomainEvents]', N'U') IS NOT NULL
BEGIN
    THROW 50000, 'Both [dbo].[events] and [dbo].[DomainEvents] exist. Move the rows out of [events] and drop it before running this script — renaming over the top would lose them.', 1;
END;

IF OBJECT_ID(N'[dbo].[events]', N'U') IS NOT NULL
BEGIN
    EXEC sp_rename N'[dbo].[events]', N'DomainEvents';
END;

IF OBJECT_ID(N'[dbo].[PK_events]', N'PK') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[PK_DomainEvents]', N'PK') IS NULL
BEGIN
    EXEC sp_rename N'[dbo].[PK_events]', N'PK_DomainEvents', N'OBJECT';
END;
