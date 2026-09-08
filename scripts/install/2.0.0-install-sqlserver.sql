/*
    Memoria 2.0.0 — event store schema install (SQL Server)

    Creates the three tables the Entity Framework Core store needs, with their keys and indexes. Run
    this to stand up a Memoria store without writing a migration yourself.

    If you manage this database with EF Core migrations, do NOT run this. Your DbContext derives from
    DomainDbContext, so `dotnet ef migrations add` generates the same schema from the model and keeps
    __EFMigrationsHistory in step. See docs/guides/install-the-store-schema.md.

    The event table is named DomainEvents from 2.0.0; before that it was `events`. To move an
    existing database across, use scripts/migrations/2.0.0-rename-events-sqlserver.sql — this script
    creates a DomainEvents table beside the old one and copies nothing.

    Assumes the default table names and the dbo schema. Adjust the identifiers below if your
    DbContext maps them elsewhere.

    Safe to run more than once: every object is guarded, so re-running adds only what is missing. It
    does not alter or drop anything that already exists — for upgrading an existing database, use the
    migration scripts under scripts/migrations instead.

    Deliberately contains no GO separators, so it runs as a single batch under any client — sqlcmd,
    SSMS, Azure Data Studio, or a plain SqlCommand.
*/

SET XACT_ABORT ON;

/* --------------------------------------------------------------- aggregates */
IF OBJECT_ID(N'[dbo].[DomainAggregates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DomainAggregates] (
        [Id] nvarchar(255) NOT NULL,
        [StreamId] nvarchar(255) NOT NULL,
        [AggregateType] nvarchar(max) NOT NULL,
        [Version] int NOT NULL,
        [LatestEventSequence] int NOT NULL,
        [Data] nvarchar(max) NOT NULL,
        [CreatedDate] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(255) NULL,
        [UpdatedDate] datetimeoffset NOT NULL,
        [UpdatedBy] nvarchar(255) NULL,
        CONSTRAINT [PK_DomainAggregates] PRIMARY KEY ([Id])
    );
END;

/* -------------------------------------------------------------- projections */
IF OBJECT_ID(N'[dbo].[DomainProjections]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DomainProjections] (
        [Id] nvarchar(255) NOT NULL,
        [StreamId] nvarchar(255) NOT NULL,
        [ProjectionType] nvarchar(max) NOT NULL,
        [Version] int NOT NULL,
        [LatestEventSequence] int NOT NULL,
        [Data] nvarchar(max) NOT NULL,
        [CreatedDate] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(255) NULL,
        [UpdatedDate] datetimeoffset NOT NULL,
        [UpdatedBy] nvarchar(255) NULL,
        CONSTRAINT [PK_DomainProjections] PRIMARY KEY ([Id])
    );
END;

/* ------------------------------------------------------------------- events */
IF OBJECT_ID(N'[dbo].[DomainEvents]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DomainEvents] (
        [Id] nvarchar(450) NOT NULL,
        [StreamId] nvarchar(255) NOT NULL,
        [EventType] nvarchar(255) NOT NULL,
        [Sequence] int NOT NULL,
        [Data] nvarchar(max) NOT NULL,
        [CreatedDate] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(255) NULL,
        CONSTRAINT [PK_DomainEvents] PRIMARY KEY ([Id])
    );
END;

/* ------------------------------------------------------------------ indexes */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_Events_EventType' AND object_id = OBJECT_ID(N'[dbo].[DomainEvents]'))
BEGIN
    CREATE INDEX [IX_Events_EventType] ON [dbo].[DomainEvents] ([EventType]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_Events_StreamId_CreatedDate' AND object_id = OBJECT_ID(N'[dbo].[DomainEvents]'))
BEGIN
    CREATE INDEX [IX_Events_StreamId_CreatedDate] ON [dbo].[DomainEvents] ([StreamId], [CreatedDate]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_Events_StreamId_Sequence' AND object_id = OBJECT_ID(N'[dbo].[DomainEvents]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Events_StreamId_Sequence] ON [dbo].[DomainEvents] ([StreamId], [Sequence]);
END;
