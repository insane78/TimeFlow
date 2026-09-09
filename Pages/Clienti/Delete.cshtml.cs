using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Clienti;

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
    public Cliente Cliente { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var cliente = await _dbContext.Clienti.FirstOrDefaultAsync(c => c.Id == id && c.UtenteId == utenteId);

        if (cliente == null)
        {
            return NotFound();
        }

        Cliente = cliente;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var cliente = await _dbContext.Clienti.FirstOrDefaultAsync(c => c.Id == Cliente.Id && c.UtenteId == utenteId);

        if (cliente == null)
        {
            return NotFound();
        }

        // Le registrazioni ore hanno FK Restrict verso Progetto/Attivita: vanno rimosse esplicitamente
        // prima di eliminare il cliente, altrimenti la cascata su Progetti/Attivita fallirebbe.
        var progettiIds = await _dbContext.Progetti
            .Where(p => p.ClienteId == cliente.Id)
            .Select(p => p.Id)
            .ToListAsync();

        var registrazioni = _dbContext.RegistrazioniOre.Where(r => progettiIds.Contains(r.ProgettoId));
        _dbContext.RegistrazioniOre.RemoveRange(registrazioni);

        _dbContext.Clienti.Remove(cliente);
        await _dbContext.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
