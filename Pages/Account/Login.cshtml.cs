using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TimeFlow.Models;

namespace TimeFlow.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<Utente> _signInManager;

    public LoginModel(SignInManager<Utente> signInManager)
    {
        _signInManager = signInManager;
    }

    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public IActionResult OnPostMicrosoft(string? returnUrl = null)
    {
        var redirectUrl = Url.Page("./ExternalLoginCallback", pageHandler: null, values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(MicrosoftAccountDefaults.AuthenticationScheme, redirectUrl);
        return Challenge(properties, MicrosoftAccountDefaults.AuthenticationScheme);
    }

    public IActionResult OnGetMicrosoft(string? returnUrl = null)
        => OnPostMicrosoft(returnUrl);
}
