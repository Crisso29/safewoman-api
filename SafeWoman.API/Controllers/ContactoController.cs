using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeWoman.Application.DTOs.Victima;
using SafeWoman.Application.Services;

namespace SafeWoman.API.Controllers;

[Route("api/contactos")]
[Authorize(Roles = "Victima")]
public class ContactoController : ApiControllerBase
{
    private readonly ContactoService _contactoService;

    public ContactoController(ContactoService contactoService) => _contactoService = contactoService;

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var contactos = await _contactoService.ListarAsync(IdVictima, ct);
        return Ok(contactos);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearContactoRequest req, CancellationToken ct)
    {
        var contacto = await _contactoService.CrearAsync(IdVictima, req, ct);
        return CreatedAtAction(nameof(Listar), null, contacto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarContactoRequest req, CancellationToken ct)
    {
        await _contactoService.ActualizarAsync(IdVictima, id, req, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _contactoService.EliminarAsync(IdVictima, id, ct);
        return NoContent();
    }
}
