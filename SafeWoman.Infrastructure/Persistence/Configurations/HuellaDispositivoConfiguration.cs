using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class HuellaDispositivoConfiguration : IEntityTypeConfiguration<HuellaDispositivo>
{
    public void Configure(EntityTypeBuilder<HuellaDispositivo> builder)
    {
        builder.ToTable("HUELLA_DISPOSITIVO");
        builder.HasKey(h => h.IdHuella);
        builder.Property(h => h.IdHuella).HasColumnName("id_huella").UseIdentityColumn();
        builder.Property(h => h.DeviceFingerprint).HasColumnName("device_fingerprint").HasMaxLength(255).IsRequired();
        builder.Property(h => h.Bloqueada).HasColumnName("bloqueada").HasDefaultValue(false);
        builder.Property(h => h.FechaPrimerUso).HasColumnName("fecha_primer_uso").HasDefaultValueSql("GETUTCDATE()");
        builder.Property(h => h.FechaUltimoUso).HasColumnName("fecha_ultimo_uso").HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(h => h.DeviceFingerprint).IsUnique().HasDatabaseName("UQ_HUELLA_fingerprint");
        builder.HasIndex(h => h.Bloqueada).HasDatabaseName("IX_HUELLA_bloqueada");
    }
}
