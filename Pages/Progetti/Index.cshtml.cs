using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Progetti;

public class IndexModel : PageModel
{
    private readonly TimeFlowDbContext _dbContext;
    private readonly UserManager<Utente> _userManager;

    public IndexModel(TimeFlowDbContext dbContext, UserManager<Utente> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public Cliente Cliente { get; set; } = null!;
    public List<Progetto> Progetti { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int clienteId)
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var cliente = await _dbContext.Clienti.FirstOrDefaultAsync(c => c.Id == clienteId && c.UtenteId == utenteId);

        if (cliente == null)
        {
            return NotFound();
        }

        Cliente = cliente;
        Progetti = await _dbContext.Progetti
            .Where(p => p.ClienteId == clienteId)
            .OrderBy(p => p.Nome)
            .ToListAsync();

        return Page();
    }
}
