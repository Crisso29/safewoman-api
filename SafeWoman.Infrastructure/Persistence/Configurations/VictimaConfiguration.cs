using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class VictimaConfiguration : IEntityTypeConfiguration<Victima>
{
    public void Configure(EntityTypeBuilder<Victima> builder)
    {
        builder.ToTable("VICTIMA");
        builder.HasKey(v => v.IdVictima);
        builder.Property(v => v.IdVictima).HasColumnName("id_victima").UseIdentityColumn();
        builder.Property(v => v.NombreCompleto).HasColumnName("nombre_completo").HasMaxLength(200).IsRequired();
        builder.Property(v => v.Dni).HasColumnName("dni").HasColumnType("CHAR(8)").IsRequired();
        builder.Property(v => v.Telefono).HasColumnName("telefono").HasMaxLength(9).IsRequired();
        builder.Property(v => v.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(v => v.Verificada).HasColumnName("verificada").HasDefaultValue(false);
        builder.Property(v => v.Activa).HasColumnName("activa").HasDefaultValue(true);
        builder.Property(v => v.FechaRegistro).HasColumnName("fecha_registro").HasDefaultValueSql("(NOW() AT TIME ZONE 'UTC')");

        builder.HasIndex(v => v.Dni).IsUnique().HasDatabaseName("UQ_VICTIMA_dni");
        builder.HasIndex(v => v.Telefono).IsUnique().HasDatabaseName("UQ_VICTIMA_telefono");

        builder.HasQueryFilter(v => v.Activa);

        // Las propiedades de navegación exponen ReadOnlyCollection — EF Core debe usar
        // el campo privado backing directamente para que el fixup de relaciones funcione.
        builder.Navigation(v => v.AlertasSos)
               .HasField("_alertas")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(v => v.ContactosEmergencia)
               .HasField("_contactos")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(v => v.Denuncias)
               .HasField("_denuncias")
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
