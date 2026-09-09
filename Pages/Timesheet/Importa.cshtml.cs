using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TimeFlow.Data;
using TimeFlow.Models;
using TimeFlow.Services;

namespace TimeFlow.Pages.Timesheet;

/// <summary>
/// Importazione massiva del timesheet da un file Excel annuale (uno sheet per mese).
/// Mappatura: colonna "Project" del file -> Cliente, colonna "Attività" del file -> Progetto.
/// L'entità Attivita di TimeFlow non viene valorizzata in questa fase.
/// </summary>
public class ImportaModel : PageModel
{
    private static readonly Regex RegexNomeFoglio = new(@"^(?<mese>\d{1,2})-(?<anno>\d{4})$", RegexOptions.Compiled);

    private readonly TimeFlowDbContext _dbContext;
    private readonly UserManager<Utente> _userManager;
    private readonly TimesheetRisoluzioneService _risoluzioneService;

    public ImportaModel(TimeFlowDbContext dbContext, UserManager<Utente> userManager, TimesheetRisoluzioneService risoluzioneService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _risoluzioneService = risoluzioneService;
    }

    [BindProperty]
    public IFormFile? File { get; set; }

    [BindProperty]
    public string? Token { get; set; }

    [BindProperty]
    public List<string> FogliSelezionati { get; set; } = new();

    public List<string> FogliDisponibili { get; set; } = new();

    public List<string> RiepilogoRisultati { get; set; } = new();

    public string? MessaggioErrore { get; set; }

    public bool ImportazioneCompletata { get; set; }

    private static string CartellaTemp => Path.Combine(Path.GetTempPath(), "TimeFlow-Import");

    private static void PulisciFileVecchi()
    {
        if (!Directory.Exists(CartellaTemp))
        {
            return;
        }

        var soglia = DateTime.UtcNow.AddHours(-2);
        foreach (var file in Directory.GetFiles(CartellaTemp, "*.xlsx"))
        {
            try
            {
                if (System.IO.File.GetLastWriteTimeUtc(file) < soglia)
                {
                    System.IO.File.Delete(file);
                }
            }
            catch (IOException)
            {
                // File in uso, verrà ripulito al prossimo giro.
            }
        }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostCaricaAsync()
    {
        if (File == null || File.Length == 0)
        {
            MessaggioErrore = "Seleziona un file Excel (.xlsx) da importare.";
            return Page();
        }

        Directory.CreateDirectory(CartellaTemp);
        PulisciFileVecchi();
        var token = Guid.NewGuid().ToString("N");
        var percorsoFile = Path.Combine(CartellaTemp, token + ".xlsx");

        await using (var stream = System.IO.File.Create(percorsoFile))
        {
            await File.CopyToAsync(stream);
        }

        try
        {
            using var workbook = new XLWorkbook(percorsoFile);
            FogliDisponibili = workbook.Worksheets
                .Select(w => w.Name)
                .Where(nome => RegexNomeFoglio.IsMatch(nome))
                .ToList();
        }
        catch (Exception)
        {
            System.IO.File.Delete(percorsoFile);
            MessaggioErrore = "Impossibile leggere il file. Verifica che sia un file Excel (.xlsx) valido.";
            return Page();
        }

        if (FogliDisponibili.Count == 0)
        {
            System.IO.File.Delete(percorsoFile);
            MessaggioErrore = "Nessun foglio con nome nel formato MM-YYYY (es. 09-2026) è stato trovato nel file.";
            return Page();
        }

        Token = token;
        return Page();
    }

    public async Task<IActionResult> OnPostImportaAsync()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            MessaggioErrore = "Sessione di importazione non valida. Ricarica il file.";
            return Page();
        }

        var percorsoFile = Path.Combine(CartellaTemp, Token + ".xlsx");
        if (!System.IO.File.Exists(percorsoFile))
        {
            MessaggioErrore = "Il file caricato non è più disponibile. Ricarica il file.";
            return Page();
        }

        if (FogliSelezionati.Count == 0)
        {
            MessaggioErrore = "Seleziona almeno un foglio da importare.";
            using var workbookRipristino = new XLWorkbook(percorsoFile);
            FogliDisponibili = workbookRipristino.Worksheets
                .Select(w => w.Name)
                .Where(nome => RegexNomeFoglio.IsMatch(nome))
                .ToList();
            return Page();
        }

        var utenteId = int.Parse(_userManager.GetUserId(User)!);

        using (var workbook = new XLWorkbook(percorsoFile))
        {
            foreach (var nomeFoglio in FogliSelezionati)
            {
                var match = RegexNomeFoglio.Match(nomeFoglio);
                if (!match.Success)
                {
                    RiepilogoRisultati.Add($"{nomeFoglio}: nome foglio non valido, saltato.");
                    continue;
                }

                var mese = int.Parse(match.Groups["mese"].Value, CultureInfo.InvariantCulture);
                var anno = int.Parse(match.Groups["anno"].Value, CultureInfo.InvariantCulture);

                if (mese < 1 || mese > 12)
                {
                    RiepilogoRisultati.Add($"{nomeFoglio}: mese non valido, saltato.");
                    continue;
                }

                if (!workbook.Worksheets.TryGetWorksheet(nomeFoglio, out var foglio))
                {
                    RiepilogoRisultati.Add($"{nomeFoglio}: foglio non trovato nel file, saltato.");
                    continue;
                }

                var righeImportate = await ImportaFoglioAsync(foglio, utenteId, anno, mese);
                RiepilogoRisultati.Add($"{nomeFoglio}: {righeImportate} registrazioni importate.");
            }
        }

        System.IO.File.Delete(percorsoFile);
        ImportazioneCompletata = true;
        return Page();
    }

    private async Task<int> ImportaFoglioAsync(IXLWorksheet foglio, int utenteId, int anno, int mese)
    {
        const int colonnaCliente = 1;
        const int colonnaProgetto = 2;
        const int primaColonnaGiorno = 3;

        var giorniNelMese = DateTime.DaysInMonth(anno, mese);
        var righeImportate = 0;

        var rigaIntestazione = TrovaRigaIntestazione(foglio, colonnaCliente);
        var primaRigaDati = rigaIntestazione + 1;
        var ultimaRiga = foglio.LastRowUsed()?.RowNumber() ?? primaRigaDati - 1;

        for (var numeroRiga = primaRigaDati; numeroRiga <= ultimaRiga; numeroRiga++)
        {
            var riga = foglio.Row(numeroRiga);

            var clienteNome = riga.Cell(colonnaCliente).GetString().Trim();
            var progettoNome = riga.Cell(colonnaProgetto).GetString().Trim();

            if (string.IsNullOrWhiteSpace(clienteNome) && string.IsNullOrWhiteSpace(progettoNome))
            {
                continue;
            }

            if (string.Equals(clienteNome, "TOTALE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(progettoNome, "TOTALE", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(clienteNome) || string.IsNullOrWhiteSpace(progettoNome))
            {
                continue;
            }

            var progettoId = await _risoluzioneService.RisolviProgettoAsync(utenteId, clienteNome, progettoNome);
            if (progettoId == null)
            {
                continue;
            }

            for (var giorno = 1; giorno <= giorniNelMese; giorno++)
            {
                var colonna = primaColonnaGiorno + giorno - 1;
                var cella = riga.Cell(colonna);

                if (cella.IsEmpty() || !cella.TryGetValue<double>(out var valoreOre) || valoreOre <= 0)
                {
                    continue;
                }

                var data = new DateOnly(anno, mese, giorno);

                var registrazione = await _dbContext.RegistrazioniOre.FirstOrDefaultAsync(r =>
                    r.UtenteId == utenteId &&
                    r.Data == data &&
                    r.ProgettoId == progettoId.Value &&
                    r.AttivitaId == null);

                if (registrazione == null)
                {
                    registrazione = new RegistrazioneOre
                    {
                        UtenteId = utenteId,
                        Data = data,
                        ProgettoId = progettoId.Value,
                        AttivitaId = null,
                        Ore = (decimal)valoreOre
                    };
                    _dbContext.RegistrazioniOre.Add(registrazione);
                }
                else
                {
                    registrazione.Ore = (decimal)valoreOre;
                }

                righeImportate++;
            }

            await _dbContext.SaveChangesAsync();
        }

        return righeImportate;
    }

    private static int TrovaRigaIntestazione(IXLWorksheet foglio, int colonnaCliente)
    {
        var ultimaRiga = foglio.LastRowUsed()?.RowNumber() ?? 1;

        for (var numeroRiga = 1; numeroRiga <= ultimaRiga; numeroRiga++)
        {
            var valore = foglio.Cell(numeroRiga, colonnaCliente).GetString().Trim();
            if (string.Equals(valore, "Project", StringComparison.OrdinalIgnoreCase))
            {
                return numeroRiga;
            }
        }

        return 1;
    }
}
