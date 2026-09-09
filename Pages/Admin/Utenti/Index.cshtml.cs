using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Admin.Utenti;

public class IndexModel : PageModel
{
    private readonly TimeFlowDbContext _dbContext;
    private readonly UserManager<Utente> _userManager;

    public IndexModel(TimeFlowDbContext dbContext, UserManager<Utente> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public List<Utente> Utenti { get; set; } = new();
    public int UtenteCorrenteId { get; set; }

    [TempData]
    public string? Messaggio { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var accessDenied = await VerificaAdminAsync();
        if (accessDenied != null)
        {
            return accessDenied;
        }

        await CaricaUtentiAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAutorizzatoAsync(int id)
    {
        var accessDenied = await VerificaAdminAsync();
        if (accessDenied != null)
        {
            return accessDenied;
        }

        var utente = await _dbContext.Users.FindAsync(id);
        if (utente == null)
        {
            return NotFound();
        }

        utente.Autorizzato = !utente.Autorizzato;
        await _dbContext.SaveChangesAsync();

        Messaggio = utente.Autorizzato
            ? $"{utente.NomeCompleto} è stato autorizzato."
            : $"L'autorizzazione di {utente.NomeCompleto} è stata revocata.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAdminAsync(int id)
    {
        var accessDenied = await VerificaAdminAsync();
        if (accessDenied != null)
        {
            return accessDenied;
        }

        var utenteCorrenteId = int.Parse(_userManager.GetUserId(User)!);
        if (id == utenteCorrenteId)
        {
            Messaggio = "Non puoi modificare i tuoi privilegi di admin.";
            return RedirectToPage();
        }

        var utente = await _dbContext.Users.FindAsync(id);
        if (utente == null)
        {
            return NotFound();
        }

        utente.Admin = !utente.Admin;
        await _dbContext.SaveChangesAsync();

        Messaggio = utente.Admin
            ? $"{utente.NomeCompleto} è ora admin."
            : $"{utente.NomeCompleto} non è più admin.";

        return RedirectToPage();
    }

    private async Task<IActionResult?> VerificaAdminAsync()
    {
        var utenteCorrenteId = int.Parse(_userManager.GetUserId(User)!);
        var isAdmin = await _dbContext.Users.AnyAsync(u => u.Id == utenteCorrenteId && u.Admin);

        if (!isAdmin)
        {
            return Forbid();
        }

        UtenteCorrenteId = utenteCorrenteId;
        return null;
    }

    private async Task CaricaUtentiAsync()
    {
        Utenti = await _dbContext.Users
            .OrderBy(u => u.NomeCompleto)
            .ToListAsync();
    }
}
