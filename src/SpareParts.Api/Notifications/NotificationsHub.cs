using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SpareParts.Api.Notifications;

[Authorize]
public sealed class NotificationsHub : Hub
{
}
