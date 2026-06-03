using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Business;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using VeterinaryClinic.Shared;
using Serilog;
using VeterinaryClinic.Business;

namespace VeterinaryClinic.API.Extensions
{
    public static class DataSeederExtension
    {
        public static async Task UseAdminSeeder(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<VeterinaryClinicDataContext>();
                    var passwordHasher = services.GetRequiredService<IBcryptPasswordHasher>();
                    var configuration = services.GetRequiredService<IConfiguration>();
                    var emailService = services.GetRequiredService<IEmailService>();

                    var adminSettings = configuration.GetSection("AdminSettings").Get<AdminSettings>();

                    if (adminSettings == null || string.IsNullOrEmpty(adminSettings.Username) || string.IsNullOrEmpty(adminSettings.Password))
                    {
                        Log.Warning("AdminSettings not configured or incomplete. Skipping admin user seeding.");
                        return;
                    }

                    // Kiểm tra xem admin đã tồn tại chưa
                    var adminUser = await context.VcUsers.FirstOrDefaultAsync(u => u.Username == adminSettings.Username);

                    if (adminUser == null)
                    {
                        // Tạo tài khoản admin
                        var newAdmin = new VcUsers
                        {
                            Code = GenerateCodeUtils.GenerateCode("ADM"),
                            Username = adminSettings.Username,
                            Email = adminSettings.Email,
                            Password = passwordHasher.HashPassword(adminSettings.Password),
                            FullName = adminSettings.FullName,
                            PhoneNumber = adminSettings.PhoneNumber,
                            Role = Role.ADMIN.ToString(),
                            IsActive = true, // Admin account should be active by default
                            CreatedDate = DateTime.UtcNow
                        };

                        await context.VcUsers.AddAsync(newAdmin);
                        await context.SaveChangesAsync();
                        Log.Information("Admin user seeded successfully.");

                        // Gửi email thông báo
                        try
                        {
                            string subject = "Thông báo cấp tài khoản - Phòng khám thú y";
                            string body = EmailTemplates.GetAccountCreatedEmail(
                                newAdmin.FullName,
                                newAdmin.Username,
                                adminSettings.Password, // Gửi mật khẩu gốc
                                newAdmin.Role
                            );

                            await emailService.SendEmailAsync(newAdmin.Email, subject, body);
                            Log.Information($"Sent account creation email to {newAdmin.Email}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Failed to send email to {newAdmin.Email}");
                        }
                    }
                    else
                    {
                        Log.Information("Admin user already exists. Skipping admin user seeding.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred while seeding the admin user.");
                }
            }
        }
    }
}
