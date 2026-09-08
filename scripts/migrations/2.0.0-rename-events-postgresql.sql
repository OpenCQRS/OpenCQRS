/*
    Memoria 2.0.0 — rename the event table to DomainEvents (PostgreSQL)

    Memoria 2.0.0 maps EventEntity to DomainEvents. Every earlier version mapped it to `events`, the
    one store table that did not say whose it was — and the name most likely to already be taken in a
    database Memoria shares with anything else. The store reads and writes DomainEvents from 2.0.0
    onwards, so an existing database must be renamed before the upgraded application runs against it.

    This is a catalogue-only rename: no rows are copied and no index is rebuilt, so it costs the same
    on a stream of ten events as on one of ten million. It does need an ACCESS EXCLUSIVE lock, so run
    it while nothing is writing.

    Assumes the default table names and the public schema. Identifiers are quoted because EF creates
    them case-sensitively; adjust them if your DbContext maps them elsewhere. Note that `events` is
    unquoted, matching what the provider generated for an already-lowercase name.

    Safe to run more than once: a database already holding DomainEvents and no `events` is left
    alone. A database holding BOTH is an error rather than a no-op — that is what running the 2.0.0
    install script before this one produces, and quietly doing nothing would strand every existing
    event in a table the store no longer reads.

    If you manage this database with EF Core migrations, do NOT run this. Your DbContext derives from
    DomainDbContext, so `dotnet ef migrations add` generates the rename from the model and keeps
    __EFMigrationsHistory in step. See docs/guides/upgrade-2.0.0.md.

    The three indexes keep their names — IX_Events_EventType, IX_Events_StreamId_CreatedDate and
    IX_Events_StreamId_Sequence — because they are named for the entity, not the table, and follow it
    across the rename. The primary key does not: EF names it after the table, so PK_events becomes
    PK_DomainEvents below. Leaving it would make a migrated database differ from one this version
    creates, which is exactly the drift the install-script comparison exists to catch.
*/

DO $$
BEGIN
    IF to_regclass('public.events') IS NOT NULL AND to_regclass('public."DomainEvents"') IS NOT NULL THEN
        RAISE EXCEPTION 'Both public.events and public."DomainEvents" exist. Move the rows out of events and drop it before running this script — renaming over the top would lose them.';
    END IF;

    IF to_regclass('public.events') IS NOT NULL THEN
        ALTER TABLE public.events RENAME TO "DomainEvents";
    END IF;

    IF EXISTS (SELECT 1 FROM pg_constraint
               WHERE conname = 'PK_events' AND conrelid = to_regclass('public."DomainEvents"'))
    THEN
        ALTER TABLE public."DomainEvents" RENAME CONSTRAINT "PK_events" TO "PK_DomainEvents";
    END IF;
END $$;
