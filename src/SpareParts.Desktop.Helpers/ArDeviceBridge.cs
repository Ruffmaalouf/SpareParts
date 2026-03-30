using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public class ArDeviceBridge : IArDeviceBridge
    {
        public bool IsConnected { get; private set; }
        public string LastConnectionDetails { get; private set; } = "AR bridge has not connected yet.";
        public string? LastFramePath { get; private set; }

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

        public async Task PushOverlayFrameAsync(ArOverlayFrame payload, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("AR bridge is not connected.");
            }

            var outputDir = Path.Combine(Path.GetTempPath(), "spareparts-ar-frames");
            Directory.CreateDirectory(outputDir);
            LastFramePath = Path.Combine(outputDir, $"frame-{payload.SessionId}.json");
            var serialized = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(LastFramePath, serialized, cancellationToken);

            LastConnectionDetails = $"Last AR payload at {DateTime.UtcNow:u}: {LastFramePath}";
        }
    }
}
