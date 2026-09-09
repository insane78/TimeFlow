namespace TimeFlow.Services;

/// <summary>
/// Interfaccia per invio email. Implementazione reale (SMTP) da configurare in seguito.
/// </summary>
public interface IEmailSender
{
    Task InviaAsync(string destinatario, string oggetto, string corpo);
}
