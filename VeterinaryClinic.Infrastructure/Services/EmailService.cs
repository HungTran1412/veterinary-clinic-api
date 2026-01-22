using MailKit.Net.Smtp;
using MimeKit;
using VeterinaryClinic.Business.Services;

namespace VeterinaryClinic.Infrastructure.Services;

public class EmailService : IEmailService
{
    // Trong thực tế, bạn nên inject IConfiguration để lấy thông tin SMTP từ appsettings.json
    private readonly string _smtpServer = "smtp.example.com";
    private readonly int _smtpPort = 587;
    private readonly string _smtpUser = "user@example.com";
    private readonly string _smtpPass = "password";

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Veterinary Clinic", _smtpUser));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        message.Body = new TextPart("html")
        {
            Text = body
        };

        using var client = new SmtpClient();
        // client.Connect(_smtpServer, _smtpPort, false);
        // client.Authenticate(_smtpUser, _smtpPass);
        // await client.SendAsync(message);
        // await client.DisconnectAsync(true);
        
        // Giả lập gửi mail thành công
        await Task.CompletedTask;
    }
}