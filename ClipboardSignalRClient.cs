using Microsoft.AspNetCore.SignalR.Client;

namespace Memoryboard
{
    public class ClipboardSignalRClient(string hubUrl, string token)
    {
        public event Action<string> BroadcastCopyReceived;
        public event Action<int> BroadcastSelectReceived;
        public event Action BroadcastClearAllReceived;

        private readonly HubConnection _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .WithAutomaticReconnect()
            .Build();

        public async Task ConnectAsync()
        {
            await _hubConnection.StartAsync();

            // Handle events from server below
            _hubConnection.On<string>("BroadcastCopy", BroadcastCopyReceived.Invoke);
            _hubConnection.On<int>("BroadcastSelect", BroadcastSelectReceived.Invoke);
            _hubConnection.On("BroadcastClearAll", BroadcastClearAllReceived);
        }

        public async Task DisconnectAsync()
        {
            await _hubConnection.StopAsync();
        }

        public void SendCopyEvent(string copiedText)
        {
            _hubConnection.InvokeAsync("Copy", copiedText);
        }

        public void SendSelectEvent(int selectedIndex)
        {
            _hubConnection.InvokeAsync("Select", selectedIndex);
        }

        public void SendClearAllEvent()
        {
            _hubConnection.InvokeAsync("ClearAll");
        }
    }
}
