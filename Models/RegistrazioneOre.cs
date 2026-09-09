namespace TimeFlow.Models;

/// <summary>
/// Registrazione delle ore lavorate da un utente in un determinato giorno,
/// per un cliente/progetto e, facoltativamente, un'attività.
/// </summary>
public class RegistrazioneOre
{
    public int Id { get; set; }

    public int UtenteId { get; set; }
    public Utente Utente { get; set; } = null!;

    public DateOnly Data { get; set; }

    public int ProgettoId { get; set; }
    public Progetto Progetto { get; set; } = null!;

    public int? AttivitaId { get; set; }
    public Attivita? Attivita { get; set; }

    public decimal Ore { get; set; }

    public string? Note { get; set; }
}
