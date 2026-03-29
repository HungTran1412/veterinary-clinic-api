using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using Serilog;
using VeterinaryClinic.Business.Services;
using VeterinaryClinic.Data;

namespace VeterinaryClinic.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public EmailService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
            var smtpPort = int.Parse(string.IsNullOrEmpty(smtpPortStr) ? "587" : smtpPortStr);
            var smtpUser = _configuration["EmailSettings:SmtpUser"];
            var smtpPass = _configuration["EmailSettings:SmtpPass"];
            var senderName = _configuration["EmailSettings:SenderName"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"];

            string errorMessage = "";
            string status = "Success";

            Log.Information($"[EmailService] Preparing to send email to {to} via {smtpServer}:{smtpPort}");

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                message.Body = new TextPart("html")
                {
                    Text = body
                };

                using var client = new SmtpClient();

                // For demo/dev environment, accept all certificates
                client.CheckCertificateRevocation = false;

                Log.Information("[EmailService] Connecting...");
                await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);

                Log.Information("[EmailService] Authenticating...");
                await client.AuthenticateAsync(smtpUser, smtpPass);

                Log.Information("[EmailService] Sending...");
                await client.SendAsync(message);

                Log.Information("[EmailService] Disconnecting...");
                await client.DisconnectAsync(true);

                Log.Information($"[EmailService] Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[EmailService] Failed to send email to {to}");
                status = "Failed";
                errorMessage = ex.Message;

                throw new Exception($"Email sending failed: {ex.Message}");
            }
            finally
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<VeterinaryClinicDataContext>();

                        var emailLog = new VcEmailLogs
                        {
                            // Sinh mã code tự động để tránh lỗi NOT NULL
                            Code = $"LOG-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                            ToEmail = to,
                            Subject = subject,
                            Body = body,
                            SentStatus = status,
                            ErrorMessage = errorMessage,
                            CreatedDate = DateTime.Now,
                            // Gán giá trị mặc định cho ReferenceType để tránh lỗi NOT NULL
                            ReferenceType = "System",
                            ReferenceId = 0
                        };

                        await dbContext.VcEmailLogs.AddAsync(emailLog);
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception dbEx)
                {
                    Log.Error(dbEx, "[EmailService] Failed to save email log to database");
                }
            }
        }
    }
}