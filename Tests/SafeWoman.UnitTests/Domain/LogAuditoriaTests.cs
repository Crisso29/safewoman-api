using FluentAssertions;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.UnitTests.Domain;

/// <summary>
/// La auditoría es un requisito legal (Ley 29733 — trazabilidad de acciones
/// sobre datos personales). Cada registro debe capturar QUIÉN hizo QUÉ y CUÁNDO.
/// </summary>
public class LogAuditoriaTests
{
    [Fact]
    public void Registrar_debe_capturar_admin_accion_entidad_y_timestamp()
    {
        var log = LogAuditoria.Registrar(
            idAdmin: 1,
            accion: AccionAuditoria.LoginAdmin,
            entidad: "ADMINISTRADOR",
            idEntidad: 1,
            descripcion: "Login desde IP 192.168.1.100");

        log.IdAdmin.Should().Be(1);
        log.Accion.Should().Be(AccionAuditoria.LoginAdmin);
        log.EntidadAfectada.Should().Be("ADMINISTRADOR");
        log.IdEntidadAfectada.Should().Be(1);
        log.Descripcion.Should().Be("Login desde IP 192.168.1.100");
        log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Registrar_debe_aceptar_acciones_de_sistema_sin_admin()
    {
        // Acciones automáticas (ej. seed inicial) no tienen admin asociado.
        var log = LogAuditoria.Registrar(
            idAdmin: null,
            accion: AccionAuditoria.ActivarVictima,
            entidad: "VICTIMA",
            idEntidad: null);

        log.IdAdmin.Should().BeNull();
        log.IdEntidadAfectada.Should().BeNull();
        log.Descripcion.Should().BeNull();
    }

    [Theory]
    [InlineData(AccionAuditoria.LoginAdmin)]
    [InlineData(AccionAuditoria.LogoutAdmin)]
    [InlineData(AccionAuditoria.CambioEstadoDenuncia)]
    [InlineData(AccionAuditoria.AtenderAlerta)]
    public void Registrar_debe_aceptar_todos_los_tipos_de_accion_definidos(AccionAuditoria accion)
    {
        var log = LogAuditoria.Registrar(1, accion, "TEST");

        log.Accion.Should().Be(accion);
    }
}
