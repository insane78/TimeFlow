namespace TimeFlow.Models;

/// <summary>
/// Cliente gestito da un utente.
/// </summary>
public class Cliente
{
    public int Id { get; set; }

    public int UtenteId { get; set; }
    public Utente Utente { get; set; } = null!;

    public string Nome { get; set; } = string.Empty;

    public bool Attivo { get; set; } = true;

    public ICollection<Progetto> Progetti { get; set; } = new List<Progetto>();
}
