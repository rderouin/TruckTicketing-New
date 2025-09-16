using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.EmailTemplates;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class EmailTemplateEntityMap : EntityMapper<EmailTemplateEntity>, IEntityMapper<EmailTemplateEntity>
{
    public override void Configure(EntityTypeBuilder<EmailTemplateEntity> modelBinding)
    {
        modelBinding.Property(x => x.ReplyType).HasConversion<string>();
        modelBinding.Property(x => x.BccType).HasConversion<string>();
        modelBinding.Property(x => x.CcType).HasConversion<string>();
    }
}
