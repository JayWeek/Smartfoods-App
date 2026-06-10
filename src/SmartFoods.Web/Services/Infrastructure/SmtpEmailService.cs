using MailKit.Net.Smtp;
using MimeKit;
using SmartFoods.Web.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace SmartFoods.Web.Services.Infrastructure;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;

    public SmtpEmailService(IConfiguration configuration)
    {
        _config = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        var fromName = _config["Smtp:FromName"] ?? "SmartFoods";
        var fromEmail = _config["Smtp:FromEmail"] ?? "no-reply@smartfoods.com";

        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        
        var host = _config["Smtp:Host"] ?? "localhost";
        var port = int.Parse(_config["Smtp:Port"] ?? "587");

        // Connect securely using STARTTLS
        await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_config["Smtp:Username"], _config["Smtp:Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
