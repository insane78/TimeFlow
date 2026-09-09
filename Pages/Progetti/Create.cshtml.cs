using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Progetti;

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
    public ProgettoInput Progetto { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int clienteId)
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var clienteEsiste = await _dbContext.Clienti.AnyAsync(c => c.Id == clienteId && c.UtenteId == utenteId);

        if (!clienteEsiste)
        {
            return NotFound();
        }

        Progetto.ClienteId = clienteId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var clienteEsiste = await _dbContext.Clienti.AnyAsync(c => c.Id == Progetto.ClienteId && c.UtenteId == utenteId);

        if (!clienteEsiste)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var progetto = new Progetto
        {
            ClienteId = Progetto.ClienteId,
            Nome = Progetto.Nome,
            Attivo = Progetto.Attivo
        };

        _dbContext.Progetti.Add(progetto);
        await _dbContext.SaveChangesAsync();

        return RedirectToPage("./Index", new { clienteId = Progetto.ClienteId });
    }

    public class ProgettoInput
    {
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Attivo")]
        public bool Attivo { get; set; } = true;
    }
}
