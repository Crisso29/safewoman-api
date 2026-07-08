namespace SafeWoman.Domain.Entities;

public class Administrador
{
    public int      IdAdmin       { get; private set; }
    public string   Nombre        { get; private set; } = default!;
    public string   Email         { get; private set; } = default!;
    public string   PasswordHash  { get; private set; } = default!;
    public bool     Activo        { get; private set; }
    public DateTime? UltimoAcceso { get; private set; }
    public DateTime FechaRegistro { get; private set; }

    private Administrador() { }

    public static Administrador Crear(string nombre, string email, string passwordHash) =>
        new()
        {
            Nombre        = nombre.Trim(),
            Email         = email.Trim().ToLowerInvariant(),
            PasswordHash  = passwordHash,
            Activo        = true,
            FechaRegistro = DateTime.UtcNow
        };

    public void RegistrarAcceso() => UltimoAcceso = DateTime.UtcNow;

    public void Desactivar() => Activo = false;
}
