using System.Threading.Tasks;
using VeterinaryClinic.Business.Models;

namespace VeterinaryClinic.Business.Services
{
    public interface INotificationService
    {
        Task SendNotificationToUser(string userId, string message);
        Task SendNotificationToAll(string message);
        Task SendAndSaveNotificationAsync(NotificationModel notification);
    }
}
