/*
    Memoria 1.9.0 — event store schema install (PostgreSQL)

    Creates the three tables the Entity Framework Core store needs, with their keys and indexes. Run
    this to stand up a Memoria store without writing a migration yourself.

    If you manage this database with EF Core migrations, do NOT run this. Your DbContext derives from
    DomainDbContext, so `dotnet ef migrations add` generates the same schema from the model and keeps
    __EFMigrationsHistory in step. See docs/guides/install-the-store-schema.md.

    The event table is named DomainEvents from 1.9.0; before that it was `events`. To move an
    existing database across, use scripts/migrations/1.9.0-rename-events-postgresql.sql — this script
    creates a DomainEvents table beside the old one and copies nothing.

    Assumes the default table names and the public schema. Identifiers are quoted because EF creates
    them case-sensitively; adjust them if your DbContext maps them elsewhere.

    Safe to run more than once: every object is created IF NOT EXISTS, so re-running adds only what is
    missing. It does not alter or drop anything that already exists — for upgrading an existing
    database, use the migration scripts under scripts/migrations instead.
*/

/* --------------------------------------------------------------- aggregates */
CREATE TABLE IF NOT EXISTS public."DomainAggregates" (
    "Id" character varying(255) NOT NULL,
    "StreamId" character varying(255) NOT NULL,
    "AggregateType" text NOT NULL,
    "Version" integer NOT NULL,
    "LatestEventSequence" integer NOT NULL,
    "Data" text NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    "UpdatedDate" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(255),
    CONSTRAINT "PK_DomainAggregates" PRIMARY KEY ("Id")
);

/* -------------------------------------------------------------- projections */
CREATE TABLE IF NOT EXISTS public."DomainProjections" (
    "Id" character varying(255) NOT NULL,
    "StreamId" character varying(255) NOT NULL,
    "ProjectionType" text NOT NULL,
    "Version" integer NOT NULL,
    "LatestEventSequence" integer NOT NULL,
    "Data" text NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    "UpdatedDate" timestamp with time zone NOT NULL,
    "UpdatedBy" character varying(255),
    CONSTRAINT "PK_DomainProjections" PRIMARY KEY ("Id")
);

/* ------------------------------------------------------------------- events */
CREATE TABLE IF NOT EXISTS public."DomainEvents" (
    "Id" text NOT NULL,
    "StreamId" character varying(255) NOT NULL,
    "EventType" character varying(255) NOT NULL,
    "Sequence" integer NOT NULL,
    "Data" text NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    CONSTRAINT "PK_DomainEvents" PRIMARY KEY ("Id")
);

/* ------------------------------------------------------------------ indexes */
CREATE INDEX IF NOT EXISTS "IX_Events_EventType"
    ON public."DomainEvents" ("EventType");

CREATE INDEX IF NOT EXISTS "IX_Events_StreamId_CreatedDate"
    ON public."DomainEvents" ("StreamId", "CreatedDate");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Events_StreamId_Sequence"
    ON public."DomainEvents" ("StreamId", "Sequence");
