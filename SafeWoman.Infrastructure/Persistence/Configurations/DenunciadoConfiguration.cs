using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class DenunciadoConfiguration : IEntityTypeConfiguration<Denunciado>
{
    public void Configure(EntityTypeBuilder<Denunciado> builder)
    {
        builder.ToTable("DENUNCIADO");
        builder.HasKey(d => d.IdDenunciado);
        builder.Property(d => d.IdDenunciado).HasColumnName("id_denunciado").UseIdentityColumn();
        builder.Property(d => d.IdDenuncia).HasColumnName("id_denuncia").IsRequired();
        builder.Property(d => d.NombreAlias).HasColumnName("nombre_alias").HasMaxLength(200).IsRequired(false);
        builder.Property(d => d.RelacionVictima).HasColumnName("relacion_victima")
               .HasConversion(
                   r => r.HasValue ? r.Value.ToString().ToLower() : null,
                   s => s != null ? Enum.Parse<RelacionVictima>(s, true) : (RelacionVictima?)null)
               .HasMaxLength(50).IsRequired(false);
    }
}
