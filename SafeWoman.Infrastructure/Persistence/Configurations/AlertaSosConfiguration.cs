using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class AlertaSosConfiguration : IEntityTypeConfiguration<AlertaSos>
{
    public void Configure(EntityTypeBuilder<AlertaSos> builder)
    {
        builder.ToTable("ALERTA_SOS");
        builder.HasKey(a => a.IdAlerta);
        builder.Property(a => a.IdAlerta).HasColumnName("id_alerta").UseIdentityColumn();
        builder.Property(a => a.IdVictima).HasColumnName("id_victima").IsRequired();
        builder.Property(a => a.Latitud).HasColumnName("latitud").HasColumnType("DECIMAL(10,7)").IsRequired();
        builder.Property(a => a.Longitud).HasColumnName("longitud").HasColumnType("DECIMAL(10,7)").IsRequired();
        builder.Property(a => a.TimestampActivacion).HasColumnName("timestamp_activacion").HasDefaultValueSql("(NOW() AT TIME ZONE 'UTC')");
        builder.Property(a => a.TimestampCancelacion).HasColumnName("timestamp_cancelacion").IsRequired(false);
        builder.Property(a => a.Estado).HasColumnName("estado")
               .HasConversion(e => e.ToString().ToLower(), s => Enum.Parse<EstadoAlerta>(s, true))
               .HasMaxLength(20).HasDefaultValue(EstadoAlerta.Activa);

        builder.HasOne(a => a.Victima)
               .WithMany(v => v.AlertasSos)
               .HasForeignKey(a => a.IdVictima)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(a => new { a.IdVictima, a.Estado })
               .HasDatabaseName("IX_ALERTA_SOS_victima_estado");
    }
}
