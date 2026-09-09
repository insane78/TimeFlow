namespace TimeFlow.Models;

/// <summary>
/// Attività (facoltativa) associata a un progetto.
/// </summary>
public class Attivita
{
    public int Id { get; set; }

    public int ProgettoId { get; set; }
    public Progetto Progetto { get; set; } = null!;

    public string Nome { get; set; } = string.Empty;

    public bool Attiva { get; set; } = true;

    public ICollection<RegistrazioneOre> RegistrazioniOre { get; set; } = new List<RegistrazioneOre>();
}
