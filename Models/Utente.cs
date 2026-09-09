using Microsoft.AspNetCore.Identity;

namespace TimeFlow.Models;

/// <summary>
/// Utente dell'applicazione. Autenticazione tramite account Microsoft.
/// </summary>
public class Utente : IdentityUser<int>
{
    public string NomeCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Indica se l'utente è stato autorizzato ad accedere alle funzionalità dell'applicazione.
    /// Di default false: un admin deve autorizzare esplicitamente.
    /// </summary>
    public bool Autorizzato { get; set; }

    /// <summary>
    /// Indica se l'utente ha privilegi di amministratore
    /// (gestione elenco utenti, autorizzazioni).
    /// </summary>
    public bool Admin { get; set; }

    public DateTime DataRegistrazione { get; set; } = DateTime.UtcNow;

    public ICollection<Cliente> Clienti { get; set; } = new List<Cliente>();
    public ICollection<RegistrazioneOre> RegistrazioniOre { get; set; } = new List<RegistrazioneOre>();
}
