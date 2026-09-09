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

        // Le registrazioni ore hanno FK Restrict verso Progetto/Attivita: vanno rimosse esplicitamente,
        // insieme ad Attivita e Progetti, prima di eliminare il cliente. Non ci si affida al cascade
        // del database perché Progetti/Attivita non sono tracciati e l'ordine delle DELETE non è garantito.
        var progetti = await _dbContext.Progetti
            .Where(p => p.ClienteId == cliente.Id)
            .ToListAsync();

        var progettiIds = progetti.Select(p => p.Id).ToList();

        var attivita = await _dbContext.Attivita
            .Where(a => progettiIds.Contains(a.ProgettoId))
            .ToListAsync();

        var registrazioni = await _dbContext.RegistrazioniOre
            .Where(r => progettiIds.Contains(r.ProgettoId))
            .ToListAsync();

        _dbContext.RegistrazioniOre.RemoveRange(registrazioni);
        _dbContext.Attivita.RemoveRange(attivita);
        _dbContext.Progetti.RemoveRange(progetti);
        _dbContext.Clienti.Remove(cliente);
        await _dbContext.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
