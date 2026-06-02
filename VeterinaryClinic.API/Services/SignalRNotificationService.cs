using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using VeterinaryClinic.Business;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Services
{
    public class SignalRNotificationService : INotificationService
    {
        private readonly IHubContext<SignalRHub> _hubContext;
        private readonly VeterinaryClinicDataContext _dataContext;

        public SignalRNotificationService(IHubContext<SignalRHub> hubContext, VeterinaryClinicDataContext dataContext)
        {
            _hubContext = hubContext;
            _dataContext = dataContext;
        }

        public async Task SendNotificationToUser(string userId, string message)
        {
            await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", message);
        }

        public async Task SendNotificationToAll(string message)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);
        }

        public async Task SendAndSaveNotificationAsync(NotificationModel notification)
        {
            // 1. Save to database
            var dbNotification = new VcNotifications
            {
                UserId = notification.UserId,
                Code = GenerateCodeUtils.GenerateCode("NOTI"),
                Title = notification.Title,
                Type = notification.Type,
                IsRead = false,
                RelatedEntityId = notification.RelatedEntityId ?? 0,
                RelatedEntityType = (int)Enum.Parse<RelatedEntityType>(notification.RelatedEntityType),
                CreatedDate = DateTime.UtcNow
            };

            await _dataContext.VcNotifications.AddAsync(dbNotification);
            await _dataContext.SaveChangesAsync();

            // 2. Send real-time notification via SignalR
            await _hubContext.Clients.Group(notification.UserId.ToString()).SendAsync("ReceiveNotification", notification.Message, dbNotification.Id);
        }
    }
}
