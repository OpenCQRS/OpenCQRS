using FluentAssertions;
using Memoria.EventSourcing.Store.EntityFrameworkCore.Containers.Tests.Fixtures;
using Memoria.EventSourcing.Store.EntityFrameworkCore.Relational.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Memoria.EventSourcing.Store.EntityFrameworkCore.Containers.Tests;

/// <summary>
/// Rehearses the upgrade a consumer actually performs: a database standing at the 1.7.0 schema, with
/// the event table still called <c>events</c>, has the 2.0.0 rename script applied to it.
/// </summary>
/// <remarks>
/// The 1.7.0 install script is the fixture precisely because the current model can no longer create
/// that table. Without it there would be nothing to rename, and the script would be shipped having
/// only ever run against a database where it was a no-op.
/// <para>
/// The rename exists so an existing store keeps its events, so the tests assert on both halves: the
/// schema it lands on is the one the model declares, and the rows that were in <c>events</c> are the
/// rows the store reads back afterwards.
/// </para>
/// </remarks>
[Trait("Category", "Container")]
public class RenameEventsScriptTests
{
    private const string StoredEventId = "rename-me:1";

    [Collection(SqlServerCollection.Name)]
    public class OnSqlServer(SqlServerFixture fixture)
    {
        private const string ScriptFileName = "2.0.0-rename-events-sqlserver.sql";

        private const string InsertOneEvent =
            """
            INSERT INTO [dbo].[events]
                ([Id], [StreamId], [EventType], [Sequence], [Data], [CreatedDate], [CreatedBy])
            VALUES
                (N'rename-me:1', N'rename-me', N'SomethingHappened:1', 1, N'{}', SYSDATETIMEOFFSET(), N'tests');
            """;

        private static Task<IReadOnlyList<string>> Describe(RelationalTestDbContext dbContext) =>
            InstallScriptComparison.DescribeAsync(dbContext,
                (context, table) => IndexMetadata.ReadSqlServerAsync(context, table, includePrimaryKeys: true));

        private async Task<RelationalTestDbContext> DatabaseAt170()
        {
            var dbContext = StoreSchema.OnSqlServer(fixture.ConnectionStringForFreshDatabase());

            await dbContext.Database.EnsureCreatedAsync();
            await MigrationScript.ExecuteAsync(dbContext,
                string.Join("\n", InstallScriptComparison.TablesInDropOrder
                    .Select(table => $"DROP TABLE [dbo].[{table}];")));
            await MigrationScript.ExecuteAsync(dbContext,
                MigrationScript.Read("1.7.0-install-sqlserver.sql", "install"));

            return dbContext;
        }

        [RequiresDockerFact]
        public async Task GivenA170Database_ThenTheScriptProducesTheSchemaTheModelDeclares()
        {
            Assert.True(fixture.IsAvailable, fixture.UnavailableReason);

            var fromModel = StoreSchema.OnSqlServer(fixture.ConnectionStringForFreshDatabase());
            var renamed = await DatabaseAt170();

            try
            {
                await fromModel.Database.EnsureCreatedAsync();
                await MigrationScript.ExecuteAsync(renamed, MigrationScript.Read(ScriptFileName));

                var expected = await Describe(fromModel);
                var actual = await Describe(renamed);

                expected.Should().NotBeEmpty("the comparison would pass vacuously against an empty schema");
                actual.Should().Equal(expected);
            }
            finally
            {
                await Discard(fromModel);
                await Discard(renamed);
            }
        }

        [RequiresDockerFact]
        public async Task GivenStoredEvents_ThenTheStoreReadsThemBackAfterTheRename()
        {
            Assert.True(fixture.IsAvailable, fixture.UnavailableReason);

            var dbContext = await DatabaseAt170();

            try
            {
                await MigrationScript.ExecuteAsync(dbContext, InsertOneEvent);
                await MigrationScript.ExecuteAsync(dbContext, MigrationScript.Read(ScriptFileName));

                (await dbContext.Events.Select(stored => stored.Id).ToListAsync())
                    .Should().ContainSingle("a rename must carry the events across, not discard them")
                    .Which.Should().Be(StoredEventId);
            }
            finally
            {
                await Discard(dbContext);
            }
        }

        [RequiresDockerFact]
        public async Task GivenTheRenameAlreadyHappened_ThenTheScriptIsStillSafeToRun()
        {
            Assert.True(fixture.IsAvailable, fixture.UnavailableReason);

            var dbContext = StoreSchema.OnSqlServer(fixture.ConnectionStringForFreshDatabase());

            try
            {
                // The 2.0.0 model creates DomainEvents and no `events`, so this is the second-run case.
                await dbContext.Database.EnsureCreatedAsync();

                var script = MigrationScript.Read(ScriptFileName);
                var run = async () => await MigrationScript.ExecuteAsync(dbContext, script);

                await run.Should().NotThrowAsync();
                await run.Should().NotThrowAsync("the script is documented as safe to run more than once");
            }
            finally
            {
                await Discard(dbContext);
            }
        }

        [RequiresDockerFact]
        public async Task GivenBothTablesExist_ThenTheScriptRefusesRatherThanStrandTheEvents()
        {
            Assert.True(fixture.IsAvailable, fixture.UnavailableReason);

            var dbContext = await DatabaseAt170();

            try
            {
                // What running the 2.0.0 install script before the rename produces.
                await MigrationScript.ExecuteAsync(dbContext,
                    MigrationScript.Read("2.0.0-install-sqlserver.sql", "install"));

                var run = async () => await MigrationScript.ExecuteAsync(dbContext, MigrationScript.Read(ScriptFileName));

                (await run.Should().ThrowAsync<Exception>()).And.Message.Should().Contain("DomainEvents");
            }
            finally
            {
                await Discard(dbContext);
            }
        }
    }

    [Collection(PostgreSqlCollection.Name)]
    public class OnPostgreSql(PostgreSqlFixture fixture)
    {
        private const string ScriptFileName = "2.0.0-rename-events-postgresql.sql";

        private const string InsertOneEvent =
            """
            INSERT INTO public.events
                ("Id", "StreamId", "EventType", "Sequence", "Data", "CreatedDate", "CreatedBy")
            VALUES
                ('rename-me:1', 'rename-me', 'SomethingHappened:1', 1, '{}', now(), 'tests');
            """;

        private static Task<IReadOnlyList<string>> Describe(RelationalTestDbContext dbContext) =>
            InstallScriptComparison.DescribeAsync(dbContext,
                (context, table) => IndexMetadata.ReadPostgreSqlAsync(context, table, includePrimaryKeys: true));

        private async Task<RelationalTestDbContext> DatabaseAt170()
        {
            var dbContext = StoreSchema.OnPostgreSql(fixture.ConnectionStringForFreshDatabase());

            await dbContext.Database.EnsureCreatedAsync();
            await MigrationScript.ExecuteAsync(dbContext,
                string.Join("\n", InstallScriptComparison.TablesInDropOrder
                    .Select(table => $"DROP TABLE public.\"{table}\";")));
            await MigrationScript.ExecuteAsync(dbContext,
                MigrationScript.Read("1.7.0-install-postgresql.sql", "install"));

            return dbContext;
        }

        [RequiresDockerFact]
        public async Task GivenA170Database_ThenTheScriptProducesTheSchemaTheModelDeclares()
        {
            Assert.True(fixture.IsAvailable, fixture.UnavailableReason);

            var fromModel = StoreSchema.OnPostgreSql(fixture.ConnectionStringForFreshDatabase());
            var renamed = await DatabaseAt170();

            try
            {
                await fromModel.Database.EnsureCreatedAsync();
                await MigrationScript.ExecuteAsync(renamed, MigrationScript.Read(ScriptFileName));

                var expected = await Describe(fromModel);
                var actual = await Describe(renamed);

                expected.Should().NotBeEmpty("the comparison would pass vacuously against an empty schema");
                actual.Should().Equal(expected);
            }
            finally
            {
                await Discard(fromModel);
                await Discard(renamed);
            }
        }

        [RequiresDockerFact]
        public async Task GivenStoredEvents_ThenTheStoreReadsThemBackAfterTheRename()
        {
            Assert.True(fixture.IsAvailable, fixture.UnavailableReason);

            var dbContext = await DatabaseAt170();

            try
            {
                await MigrationScript.ExecuteAsync(dbContext, InsertOneEvent);
                await MigrationScript.ExecuteAsync(dbContext, MigrationScript.Read(ScriptFileName));

                (await dbContext.Events.Select(stored => stored.Id).ToListAsync())
                    .Should().ContainSingle("a rename must carry the events across, not discard them")
                    .Which.Should().Be(StoredEventId);
            }
            finally
            {
                await Discard(dbContext);
            }
        }

        [RequiresDockerFact]
        public async Task GivenTheRenameAlreadyHappened_ThenTheScriptIsStillSafeToRun()
        {
            Assert.True(fixture.IsAvailable, fixture.UnavailableReason);

            var dbContext = StoreSchema.OnPostgreSql(fixture.ConnectionStringForFreshDatabase());

            try
            {
                await dbContext.Database.EnsureCreatedAsync();

                var script = MigrationScript.Read(ScriptFileName);
                var run = async () => await MigrationScript.ExecuteAsync(dbContext, script);

                await run.Should().NotThrowAsync();
                await run.Should().NotThrowAsync("the script is documented as safe to run more than once");
            }
            finally
            {
                await Discard(dbContext);
            }
        }

        [RequiresDockerFact]
        public async Task GivenBothTablesExist_ThenTheScriptRefusesRatherThanStrandTheEvents()
        {
            Assert.True(fixture.IsAvailable, fixture.UnavailableReason);

            var dbContext = await DatabaseAt170();

            try
            {
                await MigrationScript.ExecuteAsync(dbContext,
                    MigrationScript.Read("2.0.0-install-postgresql.sql", "install"));

                var run = async () => await MigrationScript.ExecuteAsync(dbContext, MigrationScript.Read(ScriptFileName));

                (await run.Should().ThrowAsync<Exception>()).And.Message.Should().Contain("DomainEvents");
            }
            finally
            {
                await Discard(dbContext);
            }
        }
    }

    private static async Task Discard(RelationalTestDbContext dbContext)
    {
        try
        {
            await dbContext.Database.EnsureDeletedAsync();
        }
        catch
        {
            // The container is discarded after the run; a failed cleanup must not mask the result.
        }

        await dbContext.DisposeAsync();
    }
}
