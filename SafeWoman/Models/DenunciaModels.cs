namespace SafeWoman.Models;

public class ContactoEmergenciaDto
{
    public int IdContacto { get; set; }
    public string Nombre { get; set; } = default!;
    public string Telefono { get; set; } = default!;
}

public class VictimaPerfilDto
{
    public int IdVictima { get; set; }
    public string NombreCompleto { get; set; } = default!;
    public string Dni { get; set; } = default!;
    public string Telefono { get; set; } = default!;
    public bool Verificada { get; set; }
    public List<ContactoEmergenciaDto> Contactos { get; set; } = [];
}

// ── Modelos para el seguimiento de "Mis denuncias" en la Home ────────────────

/// <summary>Estado de una denuncia — refleja el enum EstadoDenuncia del backend.</summary>
public enum EstadoDenuncia
{
    Pendiente,
    EnProceso,
    Atendida,
    Archivada
}

/// <summary>Denuncia formal tal como la devuelve GET /api/denuncias.</summary>
public class DenunciaDto
{
    public int IdDenuncia { get; set; }
    public EstadoDenuncia Estado { get; set; }
    public DateTime FechaEnvio { get; set; }
    public string? Departamento { get; set; }
    public string? Provincia { get; set; }
    public string? Distrito { get; set; }
    public string? Descripcion { get; set; }
}

/// <summary>Denuncia anónima resumida (sin datos sensibles).</summary>
public class DenunciaAnonimaResumenDto
{
    public int IdDenunciaAnonima { get; set; }
    public EstadoDenuncia Estado { get; set; }
    public DateTime FechaEnvio { get; set; }
    public string? Descripcion { get; set; }
}

/// <summary>
/// Item unificado que la Home muestra en la sección "Mis denuncias".
/// Convierte formal y anónima al mismo shape para simplificar la UI.
/// </summary>
public class DenunciaResumenItem
{
    public int Id { get; set; }
    public bool EsAnonima { get; set; }
    public EstadoDenuncia Estado { get; set; }
    public DateTime Fecha { get; set; }
    public string? Descripcion { get; set; }

    // ── Propiedades derivadas para el binding de UI ──────────────────────────
    public string TipoLabel     => EsAnonima ? "Anónima" : "Formal";
    public string EstadoLabel   => Estado switch
    {
        EstadoDenuncia.Pendiente => "Pendiente",
        EstadoDenuncia.EnProceso => "En proceso",
        EstadoDenuncia.Atendida  => "Atendida",
        EstadoDenuncia.Archivada => "Archivada",
        _                        => Estado.ToString()
    };
    public string EstadoIcono   => Estado switch
    {
        EstadoDenuncia.Pendiente => "⏳",
        EstadoDenuncia.EnProceso => "🔍",
        EstadoDenuncia.Atendida  => "✅",
        EstadoDenuncia.Archivada => "📁",
        _                        => "•"
    };
    public string EstadoColor   => Estado switch
    {
        EstadoDenuncia.Pendiente => "#F59E0B",  // ámbar
        EstadoDenuncia.EnProceso => "#3B82F6",  // azul
        EstadoDenuncia.Atendida  => "#10B981",  // verde
        EstadoDenuncia.Archivada => "#6B7280",  // gris
        _                        => "#6B7280"
    };
    public string FechaCorta => Fecha.ToLocalTime().ToString("dd/MM/yyyy");
    public string ResumenTexto => string.IsNullOrWhiteSpace(Descripcion)
        ? "(sin descripción)"
        : Descripcion.Length > 60 ? Descripcion[..60] + "…" : Descripcion;
}
