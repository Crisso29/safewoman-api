namespace SafeWoman.Domain.Entities;

public class Victima
{
    public int IdVictima { get; private set; }
    public string NombreCompleto { get; private set; } = default!;
    public string Dni { get; private set; } = default!;
    public string Telefono { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public bool Verificada { get; private set; }
    public bool Activa { get; private set; }
    public DateTime FechaRegistro { get; private set; }

    public IReadOnlyCollection<ContactoEmergencia> ContactosEmergencia => _contactos.AsReadOnly();
    public IReadOnlyCollection<AlertaSos> AlertasSos => _alertas.AsReadOnly();
    public IReadOnlyCollection<Denuncia> Denuncias => _denuncias.AsReadOnly();

    private readonly List<ContactoEmergencia> _contactos = [];
    private readonly List<AlertaSos> _alertas = [];
    private readonly List<Denuncia> _denuncias = [];

    private Victima() { }

    public static Victima Crear(string nombreCompleto, string dni, string telefono, string passwordHash)
    {
        return new Victima
        {
            NombreCompleto = nombreCompleto.Trim(),
            Dni = dni.Trim(),
            Telefono = telefono.Trim(),
            PasswordHash = passwordHash,
            Verificada = false,
            Activa = true,
            FechaRegistro = DateTime.UtcNow
        };
    }

    public void Verificar() => Verificada = true;

    public void ActualizarPassword(string nuevoHash) => PasswordHash = nuevoHash;

    public void Activar()    => Activa = true;
    public void Desactivar() => Activa = false;
}
