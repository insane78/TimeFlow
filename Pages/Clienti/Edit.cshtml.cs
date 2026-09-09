using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Clienti;

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
    public ClienteInput Cliente { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        var cliente = await _dbContext.Clienti.FirstOrDefaultAsync(c => c.Id == id && c.UtenteId == utenteId);

        if (cliente == null)
        {
            return NotFound();
        }

        Cliente = new ClienteInput
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            Attivo = cliente.Attivo
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
        var cliente = await _dbContext.Clienti.FirstOrDefaultAsync(c => c.Id == Cliente.Id && c.UtenteId == utenteId);

        if (cliente == null)
        {
            return NotFound();
        }

        cliente.Nome = Cliente.Nome;
        cliente.Attivo = Cliente.Attivo;

        await _dbContext.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    public class ClienteInput
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Attivo")]
        public bool Attivo { get; set; } = true;
    }
}
