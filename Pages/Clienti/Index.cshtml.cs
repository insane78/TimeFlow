using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TimeFlow.Data;
using TimeFlow.Models;

namespace TimeFlow.Pages.Clienti;

public class IndexModel : PageModel
{
    private readonly TimeFlowDbContext _dbContext;
    private readonly UserManager<Utente> _userManager;

    public IndexModel(TimeFlowDbContext dbContext, UserManager<Utente> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public List<Cliente> Clienti { get; set; } = new();

    public async Task OnGetAsync()
    {
        var utenteId = int.Parse(_userManager.GetUserId(User)!);
        Clienti = await _dbContext.Clienti
            .Where(c => c.UtenteId == utenteId)
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }
}
