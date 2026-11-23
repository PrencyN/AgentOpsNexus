using AgentOpsNexus.AgentApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentOpsNexus.AgentApi.Data.Configurations;

public sealed class AgentRequestConfiguration
    : IEntityTypeConfiguration<
        AgentRequestRecord>
{
    public void Configure(
        EntityTypeBuilder<
            AgentRequestRecord> builder)
    {
        builder.ToTable(
            "AgentRequests");

        builder.HasKey(
            request =>
                request.Id);

        builder
            .Property(
                request =>
                    request.Id)
            .ValueGeneratedNever();

        builder
            .Property(
                request =>
                    request.Message)
            .IsRequired()
            .HasMaxLength(
                4000);

        builder
            .Property(
                request =>
                    request.Environment)
            .IsRequired()
            .HasMaxLength(
                50);

        builder
            .Property(
                request =>
                    request.Severity)
            .IsRequired()
            .HasMaxLength(
                50);

        builder
            .Property(
                request =>
                    request.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(
                50);

        builder
            .Property(
                request =>
                    request.Response)
            .IsRequired();

        builder
            .Property(
                request =>
                    request.ErrorCode)
            .HasMaxLength(
                200);

        builder
            .Property(
                request =>
                    request.CorrelationId)
            .HasMaxLength(
                100);

        builder
            .Property(
                request =>
                    request.OwnerSubjectId)
            .IsRequired()
            .HasMaxLength(
                200);

        builder
            .Property(
                request =>
                    request.OwnerDisplayName)
            .IsRequired()
            .HasMaxLength(
                200);

        builder
            .Property(
                request =>
                    request.AttemptNumber)
            .IsRequired();

        builder
            .Property(
                request =>
                    request.CreatedAt)
            .IsRequired();

        builder
            .HasOne(
                request =>
                    request.RetriedFromRequest)
            .WithMany(
                request =>
                    request.RetryAttempts)
            .HasForeignKey(
                request =>
                    request.RetriedFromRequestId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder
            .HasIndex(
                request =>
                    request.CreatedAt);

        builder
            .HasIndex(
                request =>
                    request.Status);

        builder
            .HasIndex(
                request =>
                    request.CorrelationId);

        builder
            .HasIndex(
                request =>
                    request.RetriedFromRequestId);

        builder
            .HasIndex(
                request =>
                    new
                    {
                        request.OwnerSubjectId,
                        request.CreatedAt
                    });
    }
}