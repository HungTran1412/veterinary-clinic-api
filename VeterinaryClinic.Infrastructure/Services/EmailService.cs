using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using VeterinaryClinic.Business.Services;

namespace VeterinaryClinic.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpServer = _configuration["EmailSettings:SmtpServer"];
        var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
        var smtpUser = _configuration["EmailSettings:SmtpUser"];
        var smtpPass = _configuration["EmailSettings:SmtpPass"];
        var senderName = _configuration["EmailSettings:SenderName"];
        var senderEmail = _configuration["EmailSettings:SenderEmail"];

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        message.Body = new TextPart("html")
        {
            Text = body
        };

        // using var client = new SmtpClient();
        // await client.ConnectAsync(smtpServer, smtpPort, false);
        // await client.AuthenticateAsync(smtpUser, smtpPass);
        // await client.SendAsync(message);
        // await client.DisconnectAsync(true);
        
        // Giả lập gửi mail thành công
        await Task.CompletedTask;
    }
}