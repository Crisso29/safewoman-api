using SafeWoman.Domain.Enums;

namespace SafeWoman.Domain.Entities;

public class LogAuditoria
{
    public int             IdLog              { get; private set; }
    public int?            IdAdmin            { get; private set; }
    public AccionAuditoria Accion             { get; private set; }
    public string          EntidadAfectada    { get; private set; } = default!;
    public int?            IdEntidadAfectada  { get; private set; }
    public string?         Descripcion        { get; private set; }
    public DateTime        Timestamp          { get; private set; }

    public Administrador? Administrador { get; private set; }

    private LogAuditoria() { }

    public static LogAuditoria Registrar(
        int? idAdmin,
        AccionAuditoria accion,
        string entidad,
        int? idEntidad = null,
        string? descripcion = null) =>
        new()
        {
            IdAdmin           = idAdmin,
            Accion            = accion,
            EntidadAfectada   = entidad,
            IdEntidadAfectada = idEntidad,
            Descripcion       = descripcion,
            Timestamp         = DateTime.UtcNow
        };
}
