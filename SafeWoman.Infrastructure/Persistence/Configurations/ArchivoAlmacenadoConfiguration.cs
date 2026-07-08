using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class ArchivoAlmacenadoConfiguration : IEntityTypeConfiguration<ArchivoAlmacenado>
{
    public void Configure(EntityTypeBuilder<ArchivoAlmacenado> builder)
    {
        builder.ToTable("ARCHIVO");
        builder.HasKey(a => a.IdArchivo);
        builder.Property(a => a.IdArchivo).HasColumnName("id_archivo").UseIdentityColumn();

        // bytea en PostgreSQL — binario nativo, sin límite estricto de tamaño.
        // Cada blob se restringe a nivel de aplicación (max 10 MB por archivo).
        builder.Property(a => a.Contenido)
               .HasColumnName("contenido")
               .HasColumnType("bytea")
               .IsRequired();

        builder.Property(a => a.ContentType)
               .HasColumnName("content_type")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(a => a.NombreOriginal)
               .HasColumnName("nombre_original")
               .HasMaxLength(255)
               .IsRequired();

        builder.Property(a => a.Tamanio)
               .HasColumnName("tamanio_bytes")
               .IsRequired();

        builder.Property(a => a.Categoria)
               .HasColumnName("categoria")
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(a => a.FechaSubida)
               .HasColumnName("fecha_subida")
               .HasDefaultValueSql("(NOW() AT TIME ZONE 'UTC')");

        // Índice por categoría — útil para reportes ("cuántos archivos de DNI vs evidencias").
        builder.HasIndex(a => a.Categoria).HasDatabaseName("IX_ARCHIVO_categoria");
    }
}
