using DotNetCoreSqlDb.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DotNetCoreSqlDb.Hubs
{
    public interface INotificationsClient
    {
        Task AccountValidated(string userId);
    }

    [Authorize]
    public class NotificationsHub : Hub<INotificationsClient>
    {
        private readonly ILogger _logger;

        public NotificationsHub(ILogger<TodosController> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            string? userId = Context.User?.FindFirst("preferred_username")?.Value;

            _logger.LogInformation("New connection from {0}", userId);
            // TODO: Need a way to add the connection to the DB and retrieve it in the controller

            return base.OnConnectedAsync();
        }
    }
}
