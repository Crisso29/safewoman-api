using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class OtpVerificacionConfiguration : IEntityTypeConfiguration<OtpVerificacion>
{
    public void Configure(EntityTypeBuilder<OtpVerificacion> builder)
    {
        builder.ToTable("OTP_VERIFICACION");
        builder.HasKey(o => o.IdOtp);
        builder.Property(o => o.IdOtp).HasColumnName("id_otp").UseIdentityColumn();
        builder.Property(o => o.IdVictima).HasColumnName("id_victima").IsRequired();
        builder.Property(o => o.Codigo).HasColumnName("codigo").HasColumnType("CHAR(6)").IsRequired();
        builder.Property(o => o.FechaGeneracion).HasColumnName("fecha_generacion").HasDefaultValueSql("(NOW() AT TIME ZONE 'UTC')");
        builder.Property(o => o.FechaExpiracion).HasColumnName("fecha_expiracion").IsRequired();
        builder.Property(o => o.Usado).HasColumnName("usado").HasDefaultValue(false);

        builder.HasOne(o => o.Victima)
               .WithMany()
               .HasForeignKey(o => o.IdVictima)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.IdVictima, o.Usado, o.FechaExpiracion })
               .HasDatabaseName("IX_OTP_victima_usado");
    }
}
