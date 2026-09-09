using Microsoft.EntityFrameworkCore;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Services;

/// <summary>
/// Servizio condiviso per risolvere (o creare al volo) Cliente, Progetto e Attività
/// a partire dai nomi digitati/importati dall'utente.
/// </summary>
public class TimesheetRisoluzioneService
{
    private readonly TimeFlowDbContext _dbContext;

    public TimesheetRisoluzioneService(TimeFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int?> RisolviProgettoAsync(int utenteId, string? clienteNome, string? progettoNome)
    {
        if (string.IsNullOrWhiteSpace(clienteNome) || string.IsNullOrWhiteSpace(progettoNome))
        {
            return null;
        }

        clienteNome = clienteNome.Trim();
        progettoNome = progettoNome.Trim();

        var cliente = await _dbContext.Clienti.FirstOrDefaultAsync(c =>
            c.UtenteId == utenteId && c.Nome == clienteNome);

        if (cliente == null)
        {
            cliente = new Cliente { UtenteId = utenteId, Nome = clienteNome, Attivo = true };
            _dbContext.Clienti.Add(cliente);
            await _dbContext.SaveChangesAsync();
        }

        var progetto = await _dbContext.Progetti.FirstOrDefaultAsync(p =>
            p.ClienteId == cliente.Id && p.Nome == progettoNome);

        if (progetto == null)
        {
            progetto = new Progetto { ClienteId = cliente.Id, Nome = progettoNome, Attivo = true };
            _dbContext.Progetti.Add(progetto);
            await _dbContext.SaveChangesAsync();
        }

        return progetto.Id;
    }

    public async Task<int?> RisolviAttivitaAsync(int progettoId, string attivitaNome)
    {
        attivitaNome = attivitaNome.Trim();

        var attivita = await _dbContext.Attivita.FirstOrDefaultAsync(a =>
            a.ProgettoId == progettoId && a.Nome == attivitaNome);

        if (attivita == null)
        {
            attivita = new Attivita { ProgettoId = progettoId, Nome = attivitaNome, Attiva = true };
            _dbContext.Attivita.Add(attivita);
            await _dbContext.SaveChangesAsync();
        }

        return attivita.Id;
    }
}
