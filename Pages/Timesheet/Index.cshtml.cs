using System.Globalization;
using ClosedXML.Excel;
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
                Colore = c.Colore,
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
                    ClienteColore = prima.Progetto.Cliente.Colore,
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

    public async Task<IActionResult> OnGetEsportaMeseAsync(int anno, int mese)
    {
        Anno = anno;
        Mese = mese;
        CalcolaGiorni();

        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var utente = await _userManager.FindByIdAsync(utenteId.ToString());
        var nomeCompleto = utente?.NomeCompleto ?? string.Empty;

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
                    ClienteColore = prima.Progetto.Cliente.Colore,
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

        using var workbook = new XLWorkbook();
        var foglio = workbook.Worksheets.Add("Timesheet");

        const string coloreIntestazione = "#52697A";
        const string coloreTestoIntestazione = "#F8F9FA";
        const string coloreWeekend = "#D6E9FB";
        const string coloreFestivo = "#FDE2E2";
        const string coloreEtichetta = "#DCEFED";

        var colonneTotali = 3 + GiorniNelMese + 1;

        var rigaTitolo = foglio.Row(1);
        var titolo = foglio.Cell(1, 1);
        titolo.Value = $"{nomeCompleto} - Consuntivazione mensile per il mese di {MeseNome} {Anno}";
        foglio.Range(1, 1, 1, colonneTotali).Merge();
        titolo.Style.Font.Bold = true;
        titolo.Style.Font.FontSize = 14;
        titolo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        rigaTitolo.Height = 24;

        const int rigaHeader = 3;

        void ImpostaHeader(int colonna, string testo)
        {
            var cella = foglio.Cell(rigaHeader, colonna);
            cella.Value = testo;
            cella.Style.Font.Bold = true;
            cella.Style.Font.FontColor = XLColor.FromHtml(coloreTestoIntestazione);
            cella.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreIntestazione);
            cella.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cella.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cella.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        foglio.Row(rigaHeader).Height = 22;

        ImpostaHeader(1, "Cliente");
        ImpostaHeader(2, "Progetto");
        ImpostaHeader(3, "Attività");

        for (var i = 0; i < Giorni.Count; i++)
        {
            var g = Giorni[i];
            var colonna = 4 + i;
            var cella = foglio.Cell(rigaHeader, colonna);
            cella.Value = $"{g.NomeGiornoBreve} {g.Giorno}";
            cella.Style.Font.Bold = true;
            cella.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cella.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cella.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            if (g.Festivo)
            {
                cella.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreFestivo);
            }
            else if (g.Weekend)
            {
                cella.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreWeekend);
            }
            else
            {
                cella.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreIntestazione);
                cella.Style.Font.FontColor = XLColor.FromHtml(coloreTestoIntestazione);
            }

            foglio.Column(colonna).Width = 6;
        }

        ImpostaHeader(4 + GiorniNelMese, "Totale");

        var rigaCorrente = rigaHeader + 1;
        foreach (var riga in Righe)
        {
            foglio.Row(rigaCorrente).Height = 20;
            foglio.Cell(rigaCorrente, 1).Value = riga.ClienteNome;
            foglio.Cell(rigaCorrente, 2).Value = riga.ProgettoNome;
            foglio.Cell(rigaCorrente, 3).Value = riga.AttivitaNome ?? string.Empty;
            foglio.Range(rigaCorrente, 1, rigaCorrente, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var coloreRiga = !string.IsNullOrWhiteSpace(riga.ClienteColore) ? riga.ClienteColore : null;

            decimal totaleRiga = 0;
            for (var i = 0; i < GiorniNelMese; i++)
            {
                var colonna = 4 + i;
                var cella = foglio.Cell(rigaCorrente, colonna);
                var ore = riga.Ore[i];
                if (ore is not null)
                {
                    cella.Value = ore.Value;
                    totaleRiga += ore.Value;
                }
                cella.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cella.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cella.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                var giornoInfo = Giorni[i];
                if (giornoInfo.Festivo)
                {
                    cella.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreFestivo);
                }
                else if (giornoInfo.Weekend)
                {
                    cella.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreWeekend);
                }
                else if (coloreRiga is not null)
                {
                    cella.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreRiga);
                }
            }

            for (var colonna = 1; colonna <= 3; colonna++)
            {
                var cella = foglio.Cell(rigaCorrente, colonna);
                cella.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cella.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                if (coloreRiga is not null)
                {
                    cella.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreRiga);
                }
            }

            var celleTotale = foglio.Cell(rigaCorrente, 4 + GiorniNelMese);
            celleTotale.Value = totaleRiga;
            celleTotale.Style.Font.Bold = true;
            celleTotale.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreIntestazione);
            celleTotale.Style.Font.FontColor = XLColor.FromHtml(coloreTestoIntestazione);
            celleTotale.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            celleTotale.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            rigaCorrente++;
        }

        var rigaTotali = rigaCorrente;
        foglio.Row(rigaTotali).Height = 20;
        var etichettaTotali = foglio.Cell(rigaTotali, 2);
        etichettaTotali.Value = "Totale";
        etichettaTotali.Style.Font.Bold = true;
        etichettaTotali.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreEtichetta);
        etichettaTotali.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        foglio.Range(rigaTotali, 1, rigaTotali, 3).Merge();

        decimal totaleGenerale = 0;
        for (var i = 0; i < GiorniNelMese; i++)
        {
            var colonna = 4 + i;
            decimal totaleColonna = 0;
            for (var r = rigaHeader + 1; r < rigaTotali; r++)
            {
                var valore = foglio.Cell(r, colonna).GetValue<double?>();
                if (valore is not null)
                {
                    totaleColonna += (decimal)valore.Value;
                }
            }

            var cella = foglio.Cell(rigaTotali, colonna);
            if (totaleColonna > 0)
            {
                cella.Value = totaleColonna;
            }
            cella.Style.Font.Bold = true;
            cella.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreIntestazione);
            cella.Style.Font.FontColor = XLColor.FromHtml(coloreTestoIntestazione);
            cella.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cella.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            totaleGenerale += totaleColonna;
        }

        var cellaTotaleGenerale = foglio.Cell(rigaTotali, 4 + GiorniNelMese);
        cellaTotaleGenerale.Value = totaleGenerale;
        cellaTotaleGenerale.Style.Font.Bold = true;
        cellaTotaleGenerale.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreIntestazione);
        cellaTotaleGenerale.Style.Font.FontColor = XLColor.FromHtml(coloreTestoIntestazione);
        cellaTotaleGenerale.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cellaTotaleGenerale.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        foglio.Column(1).Width = 26;
        foglio.Column(2).Width = 26;
        foglio.Column(3).Width = 26;
        foglio.SheetView.FreezeRows(rigaHeader);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var nomeMeseFile = MeseNome.ToLower(new CultureInfo("it-IT"));
        var nomeFile = $"Consuntivi {nomeMeseFile} {Anno} - {nomeCompleto}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nomeFile);
    }

    public async Task<IActionResult> OnGetEsportaClienteAsync(int clienteId, int anno, int mese, bool annoIntero)
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var utente = await _userManager.FindByIdAsync(utenteId.ToString());
        var nomeCompleto = utente?.NomeCompleto ?? string.Empty;

        var cliente = await _dbContext.Clienti.FirstOrDefaultAsync(c => c.Id == clienteId && c.UtenteId == utenteId);
        if (cliente == null)
        {
            return NotFound();
        }

        DateOnly dataInizio;
        DateOnly dataFine;
        if (annoIntero)
        {
            dataInizio = new DateOnly(anno, 1, 1);
            dataFine = new DateOnly(anno, 12, 31);
        }
        else
        {
            dataInizio = new DateOnly(anno, mese, 1);
            dataFine = dataInizio.AddMonths(1).AddDays(-1);
        }

        var registrazioni = await _dbContext.RegistrazioniOre
            .Include(r => r.Progetto).ThenInclude(p => p.Cliente)
            .Include(r => r.Attivita)
            .Where(r => r.UtenteId == utenteId
                && r.Progetto.ClienteId == clienteId
                && r.Data >= dataInizio && r.Data <= dataFine)
            .OrderBy(r => r.Data)
            .ThenBy(r => r.Progetto.Nome)
            .ThenBy(r => r.Attivita != null ? r.Attivita.Nome : string.Empty)
            .ToListAsync();

        const string coloreIntestazione = "#B7F0F0";

        using var workbook = new XLWorkbook();
        var foglio = workbook.Worksheets.Add("Consuntivo");

        void ImpostaHeader(int colonna, string testo)
        {
            var cella = foglio.Cell(1, colonna);
            cella.Value = testo;
            cella.Style.Font.Bold = true;
            cella.Style.Fill.BackgroundColor = XLColor.FromHtml(coloreIntestazione);
            cella.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cella.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        foglio.Row(1).Height = 20;
        ImpostaHeader(1, "Data");
        ImpostaHeader(2, "Ore");
        ImpostaHeader(3, "Luogo");
        ImpostaHeader(4, "Fornitori coinvolti");
        ImpostaHeader(5, "Progetto");
        ImpostaHeader(6, "Attività");
        ImpostaHeader(7, "Riferimenti");

        var rigaCorrente = 2;
        decimal totaleOre = 0;
        foreach (var r in registrazioni)
        {
            foglio.Row(rigaCorrente).Height = 18;

            var cellaData = foglio.Cell(rigaCorrente, 1);
            cellaData.Value = r.Data.ToDateTime(TimeOnly.MinValue);
            cellaData.Style.DateFormat.Format = "dd/mm/yyyy";
            cellaData.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            var cellaOre = foglio.Cell(rigaCorrente, 2);
            cellaOre.Value = r.Ore;
            cellaOre.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            var cellaLuogo = foglio.Cell(rigaCorrente, 3);
            cellaLuogo.Value = "Piacenza";
            cellaLuogo.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            var cellaFornitori = foglio.Cell(rigaCorrente, 4);
            cellaFornitori.Value = cliente.Nome;
            cellaFornitori.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            var cellaProgetto = foglio.Cell(rigaCorrente, 5);
            cellaProgetto.Value = r.Progetto.Nome;
            cellaProgetto.Style.Font.Bold = true;
            cellaProgetto.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            var cellaAttivita = foglio.Cell(rigaCorrente, 6);
            cellaAttivita.Value = r.Attivita?.Nome ?? string.Empty;
            cellaAttivita.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            var cellaRiferimenti = foglio.Cell(rigaCorrente, 7);
            cellaRiferimenti.Value = string.Empty;
            cellaRiferimenti.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            totaleOre += r.Ore;
            rigaCorrente++;
        }

        var rigaTotale = rigaCorrente;
        var etichettaTotale = foglio.Cell(rigaTotale, 1);
        etichettaTotale.Value = "Totale";
        etichettaTotale.Style.Font.Bold = true;

        var cellaTotaleOre = foglio.Cell(rigaTotale, 2);
        cellaTotaleOre.Value = totaleOre;
        cellaTotaleOre.Style.Font.Bold = true;

        foglio.Column(1).Width = 14;
        foglio.Column(2).Width = 8;
        foglio.Column(3).Width = 12;
        foglio.Column(4).Width = 20;
        foglio.Column(5).Width = 40;
        foglio.Column(6).Width = 60;
        foglio.Column(7).Width = 20;
        foglio.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        string periodo;
        if (annoIntero)
        {
            periodo = anno.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            var cultura = new CultureInfo("it-IT");
            var nomeMese = cultura.DateTimeFormat.GetMonthName(mese).ToLower(cultura);
            periodo = $"{nomeMese} {anno}";
        }

        var nomeFileCliente = $"{nomeCompleto} - supporto {cliente.Nome} - {periodo}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nomeFileCliente);
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
        public string? ClienteColore { get; set; }
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
        public string? Colore { get; set; }
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
