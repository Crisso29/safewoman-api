using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class EvidenciaAnonimaConfiguration : IEntityTypeConfiguration<EvidenciaAnonima>
{
    public void Configure(EntityTypeBuilder<EvidenciaAnonima> builder)
    {
        builder.ToTable("EVIDENCIA_ANONIMA");
        builder.HasKey(e => e.IdEvidenciaAn);
        builder.Property(e => e.IdEvidenciaAn).HasColumnName("id_evidencia_an").UseIdentityColumn();
        builder.Property(e => e.IdDenunciaAnonima).HasColumnName("id_denuncia_anonima").IsRequired();
        builder.Property(e => e.NombreArchivo).HasColumnName("nombre_archivo").HasMaxLength(255).IsRequired();
        builder.Property(e => e.RutaArchivo).HasColumnName("ruta_archivo").HasMaxLength(500).IsRequired();
        builder.Property(e => e.TipoArchivo).HasColumnName("tipo_archivo")
               .HasConversion(t => t.ToString().ToLower(), s => Enum.Parse<TipoArchivo>(s, true))
               .HasMaxLength(50).HasDefaultValue(TipoArchivo.Imagen);
        builder.Property(e => e.TamanioBytes).HasColumnName("tamanio_bytes").IsRequired(false);
        builder.Property(e => e.FechaSubida).HasColumnName("fecha_subida").HasDefaultValueSql("(NOW() AT TIME ZONE 'UTC')");
    }
}
