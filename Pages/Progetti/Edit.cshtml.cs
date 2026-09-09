using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Progetti;

public class EditModel : PageModel
{
    private readonly TimeFlowDbContext _dbContext;
    private readonly UserManager<Utente> _userManager;

    public EditModel(TimeFlowDbContext dbContext, UserManager<Utente> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [BindProperty]
    public ProgettoInput Progetto { get; set; } = new();

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

        Progetto = new ProgettoInput
        {
            Id = progetto.Id,
            ClienteId = progetto.ClienteId,
            Nome = progetto.Nome,
            Attivo = progetto.Attivo
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var progetto = await _dbContext.Progetti
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == Progetto.Id && p.Cliente.UtenteId == utenteId);

        if (progetto == null)
        {
            return NotFound();
        }

        progetto.Nome = Progetto.Nome;
        progetto.Attivo = Progetto.Attivo;

        await _dbContext.SaveChangesAsync();

        return RedirectToPage("./Index", new { clienteId = progetto.ClienteId });
    }

    public class ProgettoInput
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Attivo")]
        public bool Attivo { get; set; } = true;
    }
}
