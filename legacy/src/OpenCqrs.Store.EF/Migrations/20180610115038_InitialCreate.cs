using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OpenCqrs.Store.EF.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DomainAggregate",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Type = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainAggregate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DomainCommand",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    AggregateId = table.Column<Guid>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    Data = table.Column<string>(nullable: true),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainCommand", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DomainEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    AggregateId = table.Column<Guid>(nullable: false),
                    CommandId = table.Column<Guid>(nullable: false),
                    Sequence = table.Column<int>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    Data = table.Column<string>(nullable: true),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainEvent", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DomainAggregate");

            migrationBuilder.DropTable(
                name: "DomainCommand");

            migrationBuilder.DropTable(
                name: "DomainEvent");
        }
    }
}
