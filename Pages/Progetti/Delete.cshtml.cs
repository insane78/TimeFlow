using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Progetti;

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
    public Progetto Progetto { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var progetto = await _dbContext.Progetti
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == id && p.Cliente.UtenteId == utenteId);

        if (progetto == null)
        {
            return NotFound();
        }

        Progetto = progetto;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var progetto = await _dbContext.Progetti
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == Progetto.Id && p.Cliente.UtenteId == utenteId);

        if (progetto == null)
        {
            return NotFound();
        }

        var clienteId = progetto.ClienteId;

        var registrazioni = _dbContext.RegistrazioniOre.Where(r => r.ProgettoId == progetto.Id);
        _dbContext.RegistrazioniOre.RemoveRange(registrazioni);

        var attivita = _dbContext.Attivita.Where(a => a.ProgettoId == progetto.Id);
        _dbContext.Attivita.RemoveRange(attivita);

        _dbContext.Progetti.Remove(progetto);
        await _dbContext.SaveChangesAsync();

        return RedirectToPage("./Index", new { clienteId });
    }
}
