using Microsoft.AspNetCore.Http;
using SafeWoman.Domain.Enums;

namespace SafeWoman.API.DTOs;

public class DenunciaAnonimaFormRequest
{
    public string            DeviceFingerprint     { get; set; } = default!;
    public string?           NombreAliasDenunciado { get; set; }
    public RelacionVictima?  RelacionDenunciado    { get; set; }
    public string?           Departamento          { get; set; }
    public string?           Provincia             { get; set; }
    public string?           Distrito              { get; set; }
    public string?           ReferenciaUbicacion   { get; set; }
    public decimal?          Latitud               { get; set; }
    public decimal?          Longitud              { get; set; }
    public DateOnly?         FechaHecho            { get; set; }
    public TimeOnly?         HoraHecho             { get; set; }
    public string?           Descripcion           { get; set; }
    public IFormFileCollection? Evidencias         { get; set; }
}
