using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class DenunciadoAnonimaConfiguration : IEntityTypeConfiguration<DenunciadoAnonima>
{
    public void Configure(EntityTypeBuilder<DenunciadoAnonima> builder)
    {
        builder.ToTable("DENUNCIADO_ANONIMA");
        builder.HasKey(d => d.IdDenunciadoAn);
        builder.Property(d => d.IdDenunciadoAn).HasColumnName("id_denunciado_an").UseIdentityColumn();
        builder.Property(d => d.IdDenunciaAnonima).HasColumnName("id_denuncia_anonima").IsRequired();
        builder.Property(d => d.NombreAlias).HasColumnName("nombre_alias").HasMaxLength(200).IsRequired(false);
        builder.Property(d => d.Relacion).HasColumnName("relacion")
               .HasConversion(
                   r => r.HasValue ? r.Value.ToString().ToLower() : null,
                   s => s != null ? Enum.Parse<RelacionVictima>(s, true) : (RelacionVictima?)null)
               .HasMaxLength(50).IsRequired(false);
    }
}
