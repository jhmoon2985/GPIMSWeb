using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace GPIMSWeb.Hubs
{
    [Authorize]
    public class RealtimeDataHub : Hub
    {
        public async Task JoinEquipmentGroup(string equipmentId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Equipment_{equipmentId}");
        }

        public async Task LeaveEquipmentGroup(string equipmentId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Equipment_{equipmentId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}