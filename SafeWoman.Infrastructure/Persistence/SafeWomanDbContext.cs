using Microsoft.EntityFrameworkCore;
using SafeWoman.Domain.Entities;

namespace SafeWoman.Infrastructure.Persistence;

public class SafeWomanDbContext : DbContext
{
    public SafeWomanDbContext(DbContextOptions<SafeWomanDbContext> options) : base(options) { }

    public DbSet<Administrador>       Administradores      => Set<Administrador>();
    public DbSet<Victima>             Victimas             => Set<Victima>();
    public DbSet<OtpVerificacion>     OtpVerificaciones    => Set<OtpVerificacion>();
    public DbSet<ContactoEmergencia>  ContactosEmergencia  => Set<ContactoEmergencia>();
    public DbSet<AlertaSos>           AlertasSos           => Set<AlertaSos>();
    public DbSet<Denuncia>            Denuncias            => Set<Denuncia>();
    public DbSet<Denunciado>          Denunciados          => Set<Denunciado>();
    public DbSet<Evidencia>           Evidencias           => Set<Evidencia>();
    public DbSet<HuellaDispositivo>   HuellasDispositivo   => Set<HuellaDispositivo>();
    public DbSet<DenunciaAnonima>     DenunciasAnonimas    => Set<DenunciaAnonima>();
    public DbSet<DenunciadoAnonima>   DenunciadosAnonimos  => Set<DenunciadoAnonima>();
    public DbSet<EvidenciaAnonima>    EvidenciasAnonimas   => Set<EvidenciaAnonima>();
    public DbSet<LogAuditoria>        LogsAuditoria        => Set<LogAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SafeWomanDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
