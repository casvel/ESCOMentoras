using DotNetCoreSqlDb.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DotNetCoreSqlDb.Hubs
{
    public interface IChatClient
    {
        Task ReceiveMessage(string userId, string message);
        Task Typing(string userId);
    }

    [Authorize]
    public class ChatHub : Hub<IChatClient>
    {
        private readonly ILogger _logger;

        public ChatHub(ILogger<TodosController> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            string? userId = Context.User?.FindFirst("preferred_username")?.Value;

            _logger.LogInformation("New connection from {0}", userId);

            return base.OnConnectedAsync();
        }

        public async Task SendMessage(string userId, string message)
        {
            _logger.LogInformation("Sending message from {0}: {1}", userId, message);
            await Clients.All.ReceiveMessage(userId, message);
        }

        public async Task NotifyTyping(string userId)
        {
            _logger.LogInformation("{1} is typing", userId);
            await Clients.All.Typing(userId);
        }
    }
}
