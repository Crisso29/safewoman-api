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
