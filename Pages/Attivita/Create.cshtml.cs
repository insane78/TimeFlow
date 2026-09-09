using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Attivita;

public class CreateModel : PageModel
{
    private readonly TimeFlowDbContext _dbContext;
    private readonly UserManager<Utente> _userManager;

    public CreateModel(TimeFlowDbContext dbContext, UserManager<Utente> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [BindProperty]
    public AttivitaInput Attivita { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int progettoId)
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var progettoEsiste = await _dbContext.Progetti
            .Include(p => p.Cliente)
            .AnyAsync(p => p.Id == progettoId && p.Cliente.UtenteId == utenteId);

        if (!progettoEsiste)
        {
            return NotFound();
        }

        Attivita.ProgettoId = progettoId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var progettoEsiste = await _dbContext.Progetti
            .Include(p => p.Cliente)
            .AnyAsync(p => p.Id == Attivita.ProgettoId && p.Cliente.UtenteId == utenteId);

        if (!progettoEsiste)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var attivita = new Models.Attivita
        {
            ProgettoId = Attivita.ProgettoId,
            Nome = Attivita.Nome,
            Attiva = Attivita.Attiva
        };

        _dbContext.Attivita.Add(attivita);
        await _dbContext.SaveChangesAsync();

        return RedirectToPage("./Index", new { progettoId = Attivita.ProgettoId });
    }

    public class AttivitaInput
    {
        public int ProgettoId { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Attiva")]
        public bool Attiva { get; set; } = true;
    }
}
