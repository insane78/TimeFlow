using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Attivita;

public class DeleteModel : PageModel
{
    private readonly TimeFlowDbContext _dbContext;
    private readonly UserManager<Utente> _userManager;

    public DeleteModel(TimeFlowDbContext dbContext, UserManager<Utente> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [BindProperty]
    public Models.Attivita Attivita { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var attivita = await _dbContext.Attivita
            .Include(a => a.Progetto)
            .ThenInclude(p => p.Cliente)
            .FirstOrDefaultAsync(a => a.Id == id && a.Progetto.Cliente.UtenteId == utenteId);

        if (attivita == null)
        {
            return NotFound();
        }

        Attivita = attivita;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var attivita = await _dbContext.Attivita
            .Include(a => a.Progetto)
            .ThenInclude(p => p.Cliente)
            .FirstOrDefaultAsync(a => a.Id == Attivita.Id && a.Progetto.Cliente.UtenteId == utenteId);

        if (attivita == null)
        {
            return NotFound();
        }

        var progettoId = attivita.ProgettoId;

        var registrazioni = _dbContext.RegistrazioniOre.Where(r => r.AttivitaId == attivita.Id);
        _dbContext.RegistrazioniOre.RemoveRange(registrazioni);

        _dbContext.Attivita.Remove(attivita);
        await _dbContext.SaveChangesAsync();

        return RedirectToPage("./Index", new { progettoId });
    }
}
