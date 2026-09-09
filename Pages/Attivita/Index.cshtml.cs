using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Attivita;

public class IndexModel : PageModel
{
    private readonly TimeFlowDbContext _dbContext;
    private readonly UserManager<Utente> _userManager;

    public IndexModel(TimeFlowDbContext dbContext, UserManager<Utente> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public Progetto Progetto { get; set; } = null!;
    public List<Models.Attivita> Attivita { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int progettoId)
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var progetto = await _dbContext.Progetti
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == progettoId && p.Cliente.UtenteId == utenteId);

        if (progetto == null)
        {
            return NotFound();
        }

        Progetto = progetto;
        Attivita = await _dbContext.Attivita
            .Where(a => a.ProgettoId == progettoId)
            .OrderBy(a => a.Nome)
            .ToListAsync();

        return Page();
    }
}
