using EBayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EBayClone.Infrastructure.Data.Configurations;

public class UserDocumentConfiguration : IEntityTypeConfiguration<UserDocument>
{
    public void Configure(EntityTypeBuilder<UserDocument> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName).IsRequired().HasMaxLength(255);
        builder.Property(d => d.FileUrl).IsRequired().HasMaxLength(500);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(100);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
