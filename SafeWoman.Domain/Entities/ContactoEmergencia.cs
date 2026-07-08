namespace SafeWoman.Domain.Entities;

public class ContactoEmergencia
{
    public int IdContacto { get; private set; }
    public int IdVictima { get; private set; }
    public string Nombre { get; private set; } = default!;
    public string Telefono { get; private set; } = default!;

    public Victima Victima { get; private set; } = default!;

    private ContactoEmergencia() { }

    public static ContactoEmergencia Crear(int idVictima, string nombre, string telefono)
    {
        return new ContactoEmergencia
        {
            IdVictima = idVictima,
            Nombre = nombre.Trim(),
            Telefono = telefono.Trim()
        };
    }

    public void Actualizar(string nombre, string telefono)
    {
        Nombre = nombre.Trim();
        Telefono = telefono.Trim();
    }
}
