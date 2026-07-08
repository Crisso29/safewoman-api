using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SafeWoman.Domain.Entities;
using SafeWoman.IntegrationTests.Fixtures;

namespace SafeWoman.IntegrationTests.Persistence;

/// <summary>
/// Verifica que la relación 1..N entre Victima y ContactoEmergencia
/// funciona correctamente en PostgreSQL real, incluyendo FKs, cascadas y navegaciones.
/// </summary>
[Collection("Postgres")]
public class ContactoEmergenciaPersistenceTests
{
    private readonly PostgresContainerFixture _fixture;

    public ContactoEmergenciaPersistenceTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Agregar_contactos_a_una_victima_debe_persistirlos_con_su_FK()
    {
        using var db = await _fixture.CrearDbContextAsync();
        var victima  = Victima.Crear("Ana", "12345678", "987654321", "hash");
        db.Victimas.Add(victima);
        await db.SaveChangesAsync();

        db.ContactosEmergencia.Add(ContactoEmergencia.Crear(victima.IdVictima, "María", "111111111"));
        db.ContactosEmergencia.Add(ContactoEmergencia.Crear(victima.IdVictima, "José",  "222222222"));
        db.ContactosEmergencia.Add(ContactoEmergencia.Crear(victima.IdVictima, "Lucía", "333333333"));
        await db.SaveChangesAsync();

        var contactos = await db.ContactosEmergencia
            .AsNoTracking()
            .Where(c => c.IdVictima == victima.IdVictima)
            .ToListAsync();

        contactos.Should().HaveCount(3);
        contactos.Select(c => c.Nombre).Should().Contain(["María", "José", "Lucía"]);
    }

    [Fact]
    public async Task Eliminar_un_contacto_no_debe_afectar_a_los_demas()
    {
        using var db = await _fixture.CrearDbContextAsync();
        var victima  = Victima.Crear("Ana", "12345678", "987654321", "hash");
        db.Victimas.Add(victima);
        await db.SaveChangesAsync();

        var c1 = ContactoEmergencia.Crear(victima.IdVictima, "María", "111111111");
        var c2 = ContactoEmergencia.Crear(victima.IdVictima, "José",  "222222222");
        db.ContactosEmergencia.AddRange(c1, c2);
        await db.SaveChangesAsync();

        db.ContactosEmergencia.Remove(c1);
        await db.SaveChangesAsync();

        var restantes = await db.ContactosEmergencia
            .AsNoTracking()
            .Where(c => c.IdVictima == victima.IdVictima)
            .ToListAsync();

        restantes.Should().HaveCount(1);
        restantes.Single().Nombre.Should().Be("José");
    }

    [Fact]
    public async Task Insertar_contacto_con_FK_a_victima_inexistente_debe_fallar()
    {
        // Test crítico específico de PostgreSQL: la FK a VICTIMA debe rechazar
        // inserts que apunten a un id_victima que no existe.
        // Un test con EF InMemory nunca detectaría esto porque no valida constraints.
        using var db = await _fixture.CrearDbContextAsync();

        db.ContactosEmergencia.Add(ContactoEmergencia.Crear(idVictima: 99999, "Fantasma", "+51999999999"));

        var act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "PostgreSQL debe rechazar la FK a una víctima inexistente");
    }
}
