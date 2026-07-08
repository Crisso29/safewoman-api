using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class AdministradorConfiguration : IEntityTypeConfiguration<Administrador>
{
    public void Configure(EntityTypeBuilder<Administrador> builder)
    {
        builder.ToTable("ADMINISTRADOR");
        builder.HasKey(a => a.IdAdmin);
        builder.Property(a => a.IdAdmin).HasColumnName("id_admin").UseIdentityColumn();
        builder.Property(a => a.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
        builder.Property(a => a.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(a => a.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(a => a.Activo).HasColumnName("activo").HasDefaultValue(true);
        builder.Property(a => a.UltimoAcceso).HasColumnName("ultimo_acceso").IsRequired(false);
        builder.Property(a => a.FechaRegistro).HasColumnName("fecha_registro").HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(a => a.Email).IsUnique().HasDatabaseName("UQ_ADMINISTRADOR_email");
    }
}
