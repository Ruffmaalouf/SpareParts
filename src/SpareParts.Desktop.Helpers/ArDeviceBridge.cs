using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public class ArDeviceBridge : IArDeviceBridge
    {
        public bool IsConnected { get; private set; }
        public string LastConnectionDetails { get; private set; } = "AR bridge has not connected yet.";

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(200, cancellationToken);
            IsConnected = true;
            LastConnectionDetails = $"Connected at {DateTime.UtcNow:u} (simulated bridge).";
            return true;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = false;
            LastConnectionDetails = $"Disconnected at {DateTime.UtcNow:u}.";
            return Task.CompletedTask;
        }

        public Task PushOverlayFrameAsync(string payload, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("AR bridge is not connected.");
            }

            LastConnectionDetails = $"Last AR payload at {DateTime.UtcNow:u}: {payload}";
            return Task.CompletedTask;
        }
    }
}
