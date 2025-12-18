using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try 
        {
            var smtpSection = _config.GetSection("Smtp");
            var host = smtpSection["Host"];
            var port = int.Parse(smtpSection["Port"] ?? "587");
            var user = smtpSection["User"];
            var pass = smtpSection["Pass"];
            var enableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true");

            Console.WriteLine($"SMTP Config: Host={host}, Port={port}, User={user}, SSL={enableSsl}");

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            var mailMessage = new MailMessage(user!, email, subject, htmlMessage)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mailMessage);
            Console.WriteLine($"Email sent to {email} successfully via SMTP.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SMTP Error in SmtpEmailSender: {ex.ToString()}");
            throw; // Re-throw to let caller know
        }
    }
}
