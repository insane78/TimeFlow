using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Attivita;

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
    public AttivitaInput Attivita { get; set; } = new();

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

        Attivita = new AttivitaInput
        {
            Id = attivita.Id,
            ProgettoId = attivita.ProgettoId,
            Nome = attivita.Nome,
            Attiva = attivita.Attiva
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
        var attivita = await _dbContext.Attivita
            .Include(a => a.Progetto)
            .ThenInclude(p => p.Cliente)
            .FirstOrDefaultAsync(a => a.Id == Attivita.Id && a.Progetto.Cliente.UtenteId == utenteId);

        if (attivita == null)
        {
            return NotFound();
        }

        attivita.Nome = Attivita.Nome;
        attivita.Attiva = Attivita.Attiva;

        await _dbContext.SaveChangesAsync();

        return RedirectToPage("./Index", new { progettoId = attivita.ProgettoId });
    }

    public class AttivitaInput
    {
        public int Id { get; set; }
        public int ProgettoId { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Attiva")]
        public bool Attiva { get; set; } = true;
    }
}
