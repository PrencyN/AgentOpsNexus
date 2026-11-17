using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentOpsNexus.AgentApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialRequestPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SimulateFailure = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Response = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Retryable = table.Column<bool>(type: "INTEGER", nullable: false),
                    AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RetriedFromRequestId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
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
