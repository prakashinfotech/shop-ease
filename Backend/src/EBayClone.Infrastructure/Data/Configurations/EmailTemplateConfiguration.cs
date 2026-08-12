using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Subject).IsRequired().HasMaxLength(500);
        builder.Property(t => t.HtmlBody).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(t => t.TemplateType).IsRequired();
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.Version).IsRequired();

        builder.HasQueryFilter(t => !t.IsDeleted);
        builder.HasIndex(t => new { t.TemplateType, t.IsActive });
    }
}
