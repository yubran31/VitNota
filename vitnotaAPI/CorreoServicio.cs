using System.Net;
using System.Net.Mail;

namespace VitNotaAPI;

public class CorreoServicio
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<CorreoServicio> _log;

    public CorreoServicio(IConfiguration cfg, ILogger<CorreoServicio> log)
    {
        _cfg = cfg;
        _log = log;
    }

    // Devuelve true si el correo salió. Los datos de Gmail vienen de user-secrets.
    public async Task<bool> EnviarAsync(string para, string asunto, string html)
    {
        var usuario = _cfg["Correo:Usuario"];
        var clave = _cfg["Correo:ClaveApp"];

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
        {
            _log.LogWarning("Faltan Correo:Usuario o Correo:ClaveApp en user-secrets. No se envió el correo.");
            return false;
        }

        try
        {
            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(usuario, clave.Replace(" ", ""))
            };
            using var msg = new MailMessage
            {
                From = new MailAddress(usuario, "VitNota"),
                Subject = asunto,
                Body = html,
                IsBodyHtml = true
            };
            msg.To.Add(para);

            await smtp.SendMailAsync(msg);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "No se pudo enviar el correo a {Destino}", para);
            return false;
        }
    }
}