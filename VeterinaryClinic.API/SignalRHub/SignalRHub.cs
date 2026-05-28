using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace VeterinaryClinic.API
{
    public class SignalRHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("Id")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            var userId = Context.User?.FindFirst("Id")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendToUser(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveFromUser", user, message);
        }

        public async Task SendToGroup(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveFromGroup", user, message);
        }
    }
}
