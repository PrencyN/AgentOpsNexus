using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentOpsNexus.AgentApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerDisplayName",
                table: "AgentRequests",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "Legacy Local User");

            migrationBuilder.AddColumn<string>(
                name: "OwnerSubjectId",
                table: "AgentRequests",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "legacy-local-user");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRequests_OwnerSubjectId_CreatedAt",
                table: "AgentRequests",
                columns: new[] { "OwnerSubjectId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentRequests_OwnerSubjectId_CreatedAt",
                table: "AgentRequests");

            migrationBuilder.DropColumn(
                name: "OwnerDisplayName",
                table: "AgentRequests");

            migrationBuilder.DropColumn(
                name: "OwnerSubjectId",
                table: "AgentRequests");
        }
    }
}

