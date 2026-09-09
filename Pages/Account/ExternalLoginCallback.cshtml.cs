using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TimeFlow.Data;
using TimeFlow.Models;
using TimeFlow.Services;

namespace TimeFlow.Pages.Account;

public class ExternalLoginCallbackModel : PageModel
{
    private readonly SignInManager<Utente> _signInManager;
    private readonly UserManager<Utente> _userManager;
    private readonly TimeFlowDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ExternalLoginCallbackModel> _logger;

    public ExternalLoginCallbackModel(
        SignInManager<Utente> signInManager,
        UserManager<Utente> userManager,
        TimeFlowDbContext dbContext,
        IEmailSender emailSender,
        ILogger<ExternalLoginCallbackModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToPage("./Login", new { returnUrl });
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var nome = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email ?? "Utente";

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Login esterno senza email disponibile dal provider {Provider}", info.LoginProvider);
            return RedirectToPage("./AccessDenied");
        }

        var utente = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

        if (utente == null)
        {
            // Utente già registrato con la stessa email ma senza login esterno collegato.
            utente = await _userManager.FindByEmailAsync(email);

            if (utente == null)
            {
                utente = new Utente
                {
                    UserName = email,
                    Email = email,
                    NomeCompleto = nome,
                    Autorizzato = false,
                    Admin = false,
                    DataRegistrazione = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(utente);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Errore creazione utente {Email}: {Errori}", email,
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return RedirectToPage("./AccessDenied");
                }

                await NotificaAdminNuovoUtenteAsync(utente);
            }

            var addLoginResult = await _userManager.AddLoginAsync(utente, info);
            if (!addLoginResult.Succeeded)
            {
                _logger.LogError("Errore collegamento login esterno per {Email}: {Errori}", email,
                    string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                return RedirectToPage("./AccessDenied");
            }
        }

        // Pulisce il cookie esterno temporaneo indipendentemente dall'esito.
        await _signInManager.SignOutAsync();

        if (!utente.Autorizzato)
        {
            return RedirectToPage("./InAttesaAutorizzazione");
        }

        await _signInManager.SignInAsync(utente, isPersistent: false);
        return LocalRedirect(returnUrl);
    }

    private async Task NotificaAdminNuovoUtenteAsync(Utente nuovoUtente)
    {
        var adminEmails = await _dbContext.Users
            .Where(u => u.Admin && u.Email != null)
            .Select(u => u.Email!)
            .ToListAsync();

        var oggetto = "TimeFlow - Nuova richiesta di accesso";
        var corpo = $"L'utente {nuovoUtente.NomeCompleto} ({nuovoUtente.Email}) ha richiesto l'accesso a TimeFlow e attende autorizzazione.";

        foreach (var adminEmail in adminEmails)
        {
            await _emailSender.InviaAsync(adminEmail, oggetto, corpo);
        }
    }
}
