using BlazingReceipts.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace BlazingReceipts.ReceiptWorker.Hubs
{
    public class OCRStatusHub: Hub
    {
        private readonly IConfiguration _configuration;

        public OCRStatusHub(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override Task OnConnectedAsync()
        {
            return Clients.All.SendAsync("SendAction", $"joined");
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return Clients.All.SendAsync("SendAction", $"left");
        }

        public Task Send(Receipt message)
        {
            return Clients.All.SendAsync("SendMessage", message);
        }

        public Task Receive(Receipt message)
        {
            return Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}
