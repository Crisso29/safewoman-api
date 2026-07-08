using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class ContactoEmergenciaConfiguration : IEntityTypeConfiguration<ContactoEmergencia>
{
    public void Configure(EntityTypeBuilder<ContactoEmergencia> builder)
    {
        builder.ToTable("CONTACTO_EMERGENCIA");
        builder.HasKey(c => c.IdContacto);
        builder.Property(c => c.IdContacto).HasColumnName("id_contacto").UseIdentityColumn();
        builder.Property(c => c.IdVictima).HasColumnName("id_victima").IsRequired();
        builder.Property(c => c.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
        builder.Property(c => c.Telefono).HasColumnName("telefono").HasMaxLength(9).IsRequired();

        builder.HasOne(c => c.Victima)
               .WithMany(v => v.ContactosEmergencia)
               .HasForeignKey(c => c.IdVictima)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
