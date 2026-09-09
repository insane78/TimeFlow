using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Clienti;

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
    public ClienteInput Cliente { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var utenteId = int.Parse(_userManager.GetUserId(User)!);

        var cliente = new Cliente
        {
            UtenteId = utenteId,
            Nome = Cliente.Nome,
            Attivo = Cliente.Attivo
        };

        _dbContext.Clienti.Add(cliente);
        await _dbContext.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    public class ClienteInput
    {
        [Required(ErrorMessage = "Il nome è obbligatorio.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Attivo")]
        public bool Attivo { get; set; } = true;
    }
}
