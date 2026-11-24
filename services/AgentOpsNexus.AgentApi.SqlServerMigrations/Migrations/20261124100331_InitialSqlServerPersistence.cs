using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentOpsNexus.AgentApi.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServerPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SimulateFailure = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Retryable = table.Column<bool>(type: "bit", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    RetriedFromRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OwnerSubjectId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentRequests_AgentRequests_RetriedFromRequestId",
                        column: x => x.RetriedFromRequestId,
                        principalTable: "AgentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRequests_CorrelationId",
                table: "AgentRequests",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRequests_CreatedAt",
                table: "AgentRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRequests_OwnerSubjectId_CreatedAt",
                table: "AgentRequests",
                columns: new[] { "OwnerSubjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRequests_RetriedFromRequestId",
                table: "AgentRequests",
                column: "RetriedFromRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRequests_Status",
                table: "AgentRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentRequests");
        }
    }
}
