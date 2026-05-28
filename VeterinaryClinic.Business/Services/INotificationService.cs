using System.Threading.Tasks;


namespace VeterinaryClinic.Business
{
    public interface INotificationService
    {
        Task SendNotificationToUser(string userId, string message);
        Task SendNotificationToAll(string message);
        Task SendAndSaveNotificationAsync(NotificationModel notification);
    }
}
