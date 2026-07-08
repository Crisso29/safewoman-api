using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.Infrastructure.Persistence.Configurations;

public class LogAuditoriaConfiguration : IEntityTypeConfiguration<LogAuditoria>
{
    public void Configure(EntityTypeBuilder<LogAuditoria> builder)
    {
        builder.ToTable("LOG_AUDITORIA");
        builder.HasKey(l => l.IdLog);
        builder.Property(l => l.IdLog).HasColumnName("id_log").UseIdentityColumn();
        builder.Property(l => l.IdAdmin).HasColumnName("id_admin").IsRequired(false);
        builder.Property(l => l.Accion).HasColumnName("accion")
               .HasConversion(
                   a => ToDbString(a),
                   s => FromDbString(s))
               .HasMaxLength(100).IsRequired();
        builder.Property(l => l.EntidadAfectada).HasColumnName("entidad_afectada").HasMaxLength(60).IsRequired();
        builder.Property(l => l.IdEntidadAfectada).HasColumnName("id_entidad_afectada").IsRequired(false);
        builder.Property(l => l.Descripcion).HasColumnName("descripcion").HasMaxLength(500).IsRequired(false);
        builder.Property(l => l.Timestamp).HasColumnName("timestamp").HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(l => l.Administrador)
               .WithMany()
               .HasForeignKey(l => l.IdAdmin)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => new { l.IdAdmin, l.Timestamp })
               .HasDatabaseName("IX_LOG_admin_timestamp");
    }

    // OCP: Dictionary en vez de switch — agregar una acción nueva solo requiere
    // añadir una entrada al mapa, sin modificar lógica de conversión existente.
    private static readonly Dictionary<AccionAuditoria, string> _toDb = new()
    {
        [AccionAuditoria.BloqueoDispositivo]         = "BLOQUEO_DISPOSITIVO",
        [AccionAuditoria.DesbloqueoDispositivo]      = "DESBLOQUEO_DISPOSITIVO",
        [AccionAuditoria.CambioEstadoDenuncia]       = "CAMBIO_ESTADO_DENUNCIA",
        [AccionAuditoria.CambioEstadoDenunciaAnonima]= "CAMBIO_ESTADO_DENUNCIA_ANONIMA",
        [AccionAuditoria.NotaAgregada]               = "NOTA_AGREGADA",
        [AccionAuditoria.NotaAnonimaAgregada]        = "NOTA_ANONIMA_AGREGADA",
        [AccionAuditoria.LoginAdmin]                 = "LOGIN_ADMIN",
        [AccionAuditoria.LogoutAdmin]                = "LOGOUT_ADMIN",
        [AccionAuditoria.ActivarVictima]             = "ACTIVAR_VICTIMA",
        [AccionAuditoria.DesactivarVictima]          = "DESACTIVAR_VICTIMA",
        [AccionAuditoria.BloqueoHuella]              = "BLOQUEO_HUELLA",
        [AccionAuditoria.DesbloqueoHuella]           = "DESBLOQUEO_HUELLA",
        [AccionAuditoria.AtenderAlerta]              = "ATENDER_ALERTA",
    };

    private static readonly Dictionary<string, AccionAuditoria> _fromDb =
        _toDb.ToDictionary(kv => kv.Value, kv => kv.Key);

    private static string ToDbString(AccionAuditoria a) =>
        _toDb.TryGetValue(a, out var s) ? s
        : throw new ArgumentOutOfRangeException(nameof(a), $"AccionAuditoria '{a}' no tiene mapeo DB.");

    private static AccionAuditoria FromDbString(string s) =>
        _fromDb.TryGetValue(s, out var a) ? a
        : throw new ArgumentOutOfRangeException(nameof(s), $"Valor DB '{s}' no reconocido.");
}
