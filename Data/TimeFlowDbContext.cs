using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeFlow.Models;

namespace TimeFlow.Data;

public class TimeFlowDbContext : IdentityUserContext<Utente, int>
{
    public TimeFlowDbContext(DbContextOptions<TimeFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clienti => Set<Cliente>();
    public DbSet<Progetto> Progetti => Set<Progetto>();
    public DbSet<Attivita> Attivita => Set<Attivita>();
    public DbSet<RegistrazioneOre> RegistrazioniOre => Set<RegistrazioneOre>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Cliente>()
            .HasOne(c => c.Utente)
            .WithMany(u => u.Clienti)
            .HasForeignKey(c => c.UtenteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Progetto>()
            .HasOne(p => p.Cliente)
            .WithMany(c => c.Progetti)
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Attivita>()
            .HasOne(a => a.Progetto)
            .WithMany(p => p.Attivita)
            .HasForeignKey(a => a.ProgettoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RegistrazioneOre>()
            .HasOne(r => r.Utente)
            .WithMany(u => u.RegistrazioniOre)
            .HasForeignKey(r => r.UtenteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RegistrazioneOre>()
            .HasOne(r => r.Progetto)
            .WithMany(p => p.RegistrazioniOre)
            .HasForeignKey(r => r.ProgettoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RegistrazioneOre>()
            .HasOne(r => r.Attivita)
            .WithMany(a => a.RegistrazioniOre)
            .HasForeignKey(r => r.AttivitaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RegistrazioneOre>()
            .Property(r => r.Ore)
            .HasPrecision(5, 2);
    }
}
