using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class DenunciaAnonimaConfiguration : IEntityTypeConfiguration<DenunciaAnonima>
{
    public void Configure(EntityTypeBuilder<DenunciaAnonima> builder)
    {
        builder.ToTable("DENUNCIA_ANONIMA");
        builder.HasKey(d => d.IdDenunciaAnonima);
        builder.Property(d => d.IdDenunciaAnonima).HasColumnName("id_denuncia_anonima").UseIdentityColumn();
        builder.Property(d => d.IdHuella).HasColumnName("id_huella").IsRequired();
        builder.Property(d => d.Estado).HasColumnName("estado")
               .HasConversion(e => e.ToString().ToLower(), s => Enum.Parse<EstadoDenuncia>(s, true))
               .HasMaxLength(20).HasDefaultValue(EstadoDenuncia.Pendiente);
        builder.Property(d => d.FechaEnvio).HasColumnName("fecha_envio").HasDefaultValueSql("GETUTCDATE()");
        builder.Property(d => d.Departamento).HasColumnName("departamento").HasMaxLength(100).IsRequired(false);
        builder.Property(d => d.Provincia).HasColumnName("provincia").HasMaxLength(100).IsRequired(false);
        builder.Property(d => d.Distrito).HasColumnName("distrito").HasMaxLength(100).IsRequired(false);
        builder.Property(d => d.ReferenciaUbicacion).HasColumnName("referencia_ubicacion").HasMaxLength(500).IsRequired(false);
        builder.Property(d => d.LatHecho).HasColumnName("lat_hecho").HasColumnType("DECIMAL(10,7)").IsRequired(false);
        builder.Property(d => d.LngHecho).HasColumnName("lng_hecho").HasColumnType("DECIMAL(10,7)").IsRequired(false);
        builder.Property(d => d.FechaHecho).HasColumnName("fecha_hecho").IsRequired(false);
        builder.Property(d => d.HoraHecho).HasColumnName("hora_hecho").IsRequired(false);
        builder.Property(d => d.Descripcion).HasColumnName("descripcion").HasColumnType("TEXT").IsRequired(false);

        builder.HasOne(d => d.HuellaDispositivo)
               .WithMany()
               .HasForeignKey(d => d.IdHuella)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.Denunciado)
               .WithOne(dn => dn.DenunciaAnonima)
               .HasForeignKey<DenunciadoAnonima>(dn => dn.IdDenunciaAnonima)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Evidencias)
               .WithOne(e => e.DenunciaAnonima)
               .HasForeignKey(e => e.IdDenunciaAnonima)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.Estado, d.FechaEnvio }).HasDatabaseName("IX_DENUNCIA_ANONIMA_estado_fecha");
        builder.HasIndex(d => d.IdHuella).HasDatabaseName("IX_DENUNCIA_ANONIMA_huella");

        // Evidencias expone ReadOnlyCollection — EF Core usa el campo privado backing para fixup
        builder.Navigation(d => d.Evidencias)
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
