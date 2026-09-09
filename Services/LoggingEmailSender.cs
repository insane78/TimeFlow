using Microsoft.Extensions.Logging;

namespace TimeFlow.Services;

/// <summary>
/// Implementazione temporanea che si limita a loggare l'email invece di inviarla.
/// Da sostituire con un invio SMTP reale quando disponibile.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task InviaAsync(string destinatario, string oggetto, string corpo)
    {
        _logger.LogInformation("Email a {Destinatario} - Oggetto: {Oggetto}\n{Corpo}", destinatario, oggetto, corpo);
        return Task.CompletedTask;
    }
}
