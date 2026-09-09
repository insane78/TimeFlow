namespace TimeFlow.Models;

/// <summary>
/// Progetto associato a un cliente.
/// </summary>
public class Progetto
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public string Nome { get; set; } = string.Empty;

    public bool Attivo { get; set; } = true;

    public ICollection<Attivita> Attivita { get; set; } = new List<Attivita>();
    public ICollection<RegistrazioneOre> RegistrazioniOre { get; set; } = new List<RegistrazioneOre>();
}
