using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.MaterialApproval;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class MaterialApprovalEntityMapping : EntityMapper<MaterialApprovalEntity>, IEntityMapper<MaterialApprovalEntity>
{
    public override void Configure(EntityTypeBuilder<MaterialApprovalEntity> modelBinding)
    {
        modelBinding.Property(x => x.CountryCode).HasConversion<string>();
        modelBinding.Property(x => x.SourceRegion).HasConversion<string>();
        modelBinding.Property(x => x.HazardousNonhazardous).HasConversion<string>();
        modelBinding.Property(x => x.LoadSummaryReportFrequencyWeekDay).HasConversion<string>();
        modelBinding.Property(x => x.LoadSummaryReportFrequency).HasConversion<string>();
        modelBinding.Property(e => e.DownHoleType).HasConversion<string>();

        modelBinding.OwnsMany(x => x.ApplicantSignatories, ApplicantSignatoryBuilder =>
                                                           {
                                                               ApplicantSignatoryBuilder.WithOwner();
                                                           });

        modelBinding.OwnsMany(x => x.LoadSummaryReportRecipients, ReportRecipientBuilder =>
                                                                  {
                                                                      ReportRecipientBuilder.WithOwner();
                                                                  });
    }
}

public class ApplicantSignatoryEntityMap : EntityMapper<ApplicantSignatoryEntity>, IEntityMapper<ApplicantSignatoryEntity>
{
    public override void Configure(EntityTypeBuilder<ApplicantSignatoryEntity> modelBinding)
    {
    }
}

public class LoadSummaryReportRecipientEntityMap : EntityMapper<LoadSummaryReportRecipientEntity>, IEntityMapper<LoadSummaryReportRecipientEntity>
{
    public override void Configure(EntityTypeBuilder<LoadSummaryReportRecipientEntity> modelBinding)
    {
    }
}
