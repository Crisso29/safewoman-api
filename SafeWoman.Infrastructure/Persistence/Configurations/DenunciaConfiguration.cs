using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class DenunciaConfiguration : IEntityTypeConfiguration<Denuncia>
{
    public void Configure(EntityTypeBuilder<Denuncia> builder)
    {
        builder.ToTable("DENUNCIA");
        builder.HasKey(d => d.IdDenuncia);
        builder.Property(d => d.IdDenuncia).HasColumnName("id_denuncia").UseIdentityColumn();
        builder.Property(d => d.IdVictima).HasColumnName("id_victima").IsRequired();
        builder.Property(d => d.Tipo).HasColumnName("tipo")
               .HasConversion(t => t.ToString().ToLower(), s => Enum.Parse<TipoDenuncia>(s, true))
               .HasMaxLength(20).IsRequired();
        builder.Property(d => d.Estado).HasColumnName("estado")
               .HasConversion(e => e.ToString().ToLower(), s => Enum.Parse<EstadoDenuncia>(s, true))
               .HasMaxLength(20).HasDefaultValue(EstadoDenuncia.Pendiente);
        builder.Property(d => d.FechaEnvio).HasColumnName("fecha_envio").HasDefaultValueSql("GETUTCDATE()");
        builder.Property(d => d.FotoDniRuta).HasColumnName("foto_dni_ruta").HasMaxLength(500).IsRequired(false);
        builder.Property(d => d.Departamento).HasColumnName("departamento").HasMaxLength(100).IsRequired(false);
        builder.Property(d => d.Provincia).HasColumnName("provincia").HasMaxLength(100).IsRequired(false);
        builder.Property(d => d.Distrito).HasColumnName("distrito").HasMaxLength(100).IsRequired(false);
        builder.Property(d => d.ReferenciaUbicacion).HasColumnName("referencia_ubicacion").HasMaxLength(500).IsRequired(false);
        builder.Property(d => d.LatHecho).HasColumnName("lat_hecho").HasColumnType("DECIMAL(10,7)").IsRequired(false);
        builder.Property(d => d.LngHecho).HasColumnName("lng_hecho").HasColumnType("DECIMAL(10,7)").IsRequired(false);
        builder.Property(d => d.FechaHecho).HasColumnName("fecha_hecho").IsRequired(false);
        builder.Property(d => d.HoraHecho).HasColumnName("hora_hecho").IsRequired(false);
        builder.Property(d => d.Descripcion).HasColumnName("descripcion").HasColumnType("TEXT").IsRequired(false);
        builder.Property(d => d.DeclaracionJurada).HasColumnName("declaracion_jurada").HasDefaultValue(false);

        builder.HasOne(d => d.Victima)
               .WithMany(v => v.Denuncias)
               .HasForeignKey(d => d.IdVictima)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.Denunciado)
               .WithOne(dn => dn.Denuncia)
               .HasForeignKey<Denunciado>(dn => dn.IdDenuncia)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Evidencias)
               .WithOne(e => e.Denuncia)
               .HasForeignKey(e => e.IdDenuncia)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.Estado, d.Tipo, d.FechaEnvio })
               .HasDatabaseName("IX_DENUNCIA_estado_tipo_fecha");
        builder.HasIndex(d => d.IdVictima).HasDatabaseName("IX_DENUNCIA_victima");

        // Evidencias expone ReadOnlyCollection — EF Core usa el campo privado backing para fixup
        builder.Navigation(d => d.Evidencias)
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
