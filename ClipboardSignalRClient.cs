using Microsoft.AspNetCore.SignalR.Client;

namespace Memoryboard
{
    public class ClipboardSignalRClient(string hubUrl, string token)
    {
        public event Action<byte[]> BroadcastCopyReceived;
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
            _hubConnection.On<byte[]>("BroadcastCopy", BroadcastCopyReceived.Invoke);
            _hubConnection.On<int>("BroadcastSelect", BroadcastSelectReceived.Invoke);
            _hubConnection.On("BroadcastClearAll", BroadcastClearAllReceived);
        }

        public async Task DisconnectAsync()
        {
            await _hubConnection.StopAsync();
        }

        public void SendCopyEvent(byte[] encryptedBytes)
        {
            _hubConnection.InvokeAsync("Copy", encryptedBytes);
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
