using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Timesheet;

public class IndexModel : PageModel
{
    private readonly TimeFlowDbContext _dbContext;
    private readonly UserManager<Utente> _userManager;

    public IndexModel(TimeFlowDbContext dbContext, UserManager<Utente> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public int Anno { get; set; }
    public int Mese { get; set; }
    public string MeseNome { get; set; } = string.Empty;
    public int GiorniNelMese { get; set; }
    public List<GiornoInfo> Giorni { get; set; } = new();
    public List<RigaTimesheet> Righe { get; set; } = new();
    public List<ClienteDto> Clienti { get; set; } = new();

    public async Task OnGetAsync(int? anno, int? mese)
    {
        var oggi = DateTime.Today;
        Anno = anno ?? oggi.Year;
        Mese = mese ?? oggi.Month;

        CalcolaGiorni();

        var utenteId = int.Parse(_userManager.GetUserId(User)!);

        Clienti = await _dbContext.Clienti
            .Where(c => c.UtenteId == utenteId)
            .OrderBy(c => c.Nome)
            .Select(c => new ClienteDto
            {
                Id = c.Id,
                Nome = c.Nome,
                Progetti = c.Progetti.OrderBy(p => p.Nome).Select(p => new ProgettoDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Attivita = p.Attivita.OrderBy(a => a.Nome).Select(a => new AttivitaDto
                    {
                        Id = a.Id,
                        Nome = a.Nome
                    }).ToList()
                }).ToList()
            })
            .ToListAsync();

        var primoGiorno = new DateOnly(Anno, Mese, 1);
        var ultimoGiorno = primoGiorno.AddMonths(1).AddDays(-1);

        var registrazioni = await _dbContext.RegistrazioniOre
            .Include(r => r.Progetto).ThenInclude(p => p.Cliente)
            .Include(r => r.Attivita)
            .Where(r => r.UtenteId == utenteId && r.Data >= primoGiorno && r.Data <= ultimoGiorno)
            .ToListAsync();

        Righe = registrazioni
            .GroupBy(r => new { r.ProgettoId, r.AttivitaId })
            .Select(g =>
            {
                var prima = g.First();
                var riga = new RigaTimesheet
                {
                    ClienteId = prima.Progetto.ClienteId,
                    ClienteNome = prima.Progetto.Cliente.Nome,
                    ProgettoId = prima.ProgettoId,
                    ProgettoNome = prima.Progetto.Nome,
                    AttivitaId = prima.AttivitaId,
                    AttivitaNome = prima.Attivita?.Nome,
                    Ore = new decimal?[GiorniNelMese]
                };

                foreach (var r in g)
                {
                    riga.Ore[r.Data.Day - 1] = r.Ore;
                }

                return riga;
            })
            .OrderBy(r => r.ClienteNome).ThenBy(r => r.ProgettoNome).ThenBy(r => r.AttivitaNome)
            .ToList();
    }

    public async Task<IActionResult> OnPostSalvaCellaAsync([FromBody] SalvaCellaRequest request)
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);

        var progettoId = await RisolviProgettoAsync(utenteId, request.ClienteNome, request.ProgettoNome);
        if (progettoId == null)
        {
            return BadRequest(new { errore = "Cliente o progetto non validi." });
        }

        int? attivitaId = null;
        if (!string.IsNullOrWhiteSpace(request.AttivitaNome))
        {
            attivitaId = await RisolviAttivitaAsync(progettoId.Value, request.AttivitaNome);
        }

        var data = new DateOnly(request.Anno, request.Mese, request.Giorno);

        var registrazione = await _dbContext.RegistrazioniOre.FirstOrDefaultAsync(r =>
            r.UtenteId == utenteId &&
            r.Data == data &&
            r.ProgettoId == progettoId.Value &&
            r.AttivitaId == attivitaId);

        if (request.Ore is null || request.Ore <= 0)
        {
            if (registrazione != null)
            {
                _dbContext.RegistrazioniOre.Remove(registrazione);
                await _dbContext.SaveChangesAsync();
            }

            return new JsonResult(new { ok = true, progettoId, attivitaId });
        }

        if (registrazione == null)
        {
            registrazione = new RegistrazioneOre
            {
                UtenteId = utenteId,
                Data = data,
                ProgettoId = progettoId.Value,
                AttivitaId = attivitaId,
                Ore = request.Ore.Value
            };
            _dbContext.RegistrazioniOre.Add(registrazione);
        }
        else
        {
            registrazione.Ore = request.Ore.Value;
        }

        await _dbContext.SaveChangesAsync();

        return new JsonResult(new { ok = true, progettoId, attivitaId });
    }

    private async Task<int?> RisolviProgettoAsync(int utenteId, string? clienteNome, string? progettoNome)
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

    private async Task<int?> RisolviAttivitaAsync(int progettoId, string attivitaNome)
    {
        attivitaNome = attivitaNome.Trim();

        var attivita = await _dbContext.Attivita.FirstOrDefaultAsync(a =>
            a.ProgettoId == progettoId && a.Nome == attivitaNome);

        if (attivita == null)
        {
            attivita = new TimeFlow.Models.Attivita { ProgettoId = progettoId, Nome = attivitaNome, Attiva = true };
            _dbContext.Attivita.Add(attivita);
            await _dbContext.SaveChangesAsync();
        }

        return attivita.Id;
    }

    private void CalcolaGiorni()
    {
        var cultura = new CultureInfo("it-IT");
        MeseNome = cultura.DateTimeFormat.GetMonthName(Mese);
        GiorniNelMese = DateTime.DaysInMonth(Anno, Mese);

        var festivita = CalcolaFestivita(Anno);

        Giorni = new List<GiornoInfo>();
        for (var giorno = 1; giorno <= GiorniNelMese; giorno++)
        {
            var data = new DateOnly(Anno, Mese, giorno);
            var weekend = data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            Giorni.Add(new GiornoInfo
            {
                Giorno = giorno,
                NomeGiornoBreve = cultura.DateTimeFormat.GetAbbreviatedDayName(data.DayOfWeek),
                Weekend = weekend,
                Festivo = festivita.Contains(data)
            });
        }
    }

    private static HashSet<DateOnly> CalcolaFestivita(int anno)
    {
        var pasqua = CalcolaPasqua(anno);

        var festivita = new HashSet<DateOnly>
        {
            new(anno, 1, 1),
            new(anno, 1, 6),
            pasqua,
            pasqua.AddDays(1),
            new(anno, 4, 25),
            new(anno, 5, 1),
            new(anno, 6, 2),
            new(anno, 8, 15),
            new(anno, 11, 1),
            new(anno, 12, 8),
            new(anno, 12, 25),
            new(anno, 12, 26)
        };

        return festivita;
    }

    private static DateOnly CalcolaPasqua(int anno)
    {
        // Algoritmo di Gauss per il calcolo della Pasqua.
        var a = anno % 19;
        var b = anno / 100;
        var c = anno % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var mese = (h + l - 7 * m + 114) / 31;
        var giorno = (h + l - 7 * m + 114) % 31 + 1;

        return new DateOnly(anno, mese, giorno);
    }

    public class GiornoInfo
    {
        public int Giorno { get; set; }
        public string NomeGiornoBreve { get; set; } = string.Empty;
        public bool Weekend { get; set; }
        public bool Festivo { get; set; }
    }

    public class RigaTimesheet
    {
        public int ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public int ProgettoId { get; set; }
        public string ProgettoNome { get; set; } = string.Empty;
        public int? AttivitaId { get; set; }
        public string? AttivitaNome { get; set; }
        public decimal?[] Ore { get; set; } = Array.Empty<decimal?>();
    }

    public class ClienteDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public List<ProgettoDto> Progetti { get; set; } = new();
    }

    public class ProgettoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public List<AttivitaDto> Attivita { get; set; } = new();
    }

    public class AttivitaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public class SalvaCellaRequest
    {
        public int Anno { get; set; }
        public int Mese { get; set; }
        public int Giorno { get; set; }
        public string? ClienteNome { get; set; }
        public string? ProgettoNome { get; set; }
        public string? AttivitaNome { get; set; }
        public decimal? Ore { get; set; }
    }
}
