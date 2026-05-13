using Microsoft.AspNetCore.SignalR;

namespace VeterinaryClinic.API;

public class SignalRHub : Hub
{ 
    public async Task SendToUser(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveFromUser", user, message);
    }
    public async Task SendToGroup(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveFromGroup", user, message);
    }
}