using System.Text.Json;
using Vyre.App.Models;
using Vyre.App.Services.Engine;

namespace Vyre.App.Services.Capture;

public sealed class PacketCaptureService : IPacketCaptureService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<CaptureDeviceModel>> ListDevicesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            using var doc = JsonDocument.Parse(NativeMethods.ListCaptureDevicesJson());
            var devices = new List<CaptureDeviceModel>();

            if (doc.RootElement.TryGetProperty("devices", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    devices.Add(new CaptureDeviceModel
                    {
                        Name = item.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                        Description = item.TryGetProperty("description", out var description) ? description.GetString() ?? string.Empty : string.Empty
                    });
                }
            }

            return (IReadOnlyList<CaptureDeviceModel>)devices;
        }, cancellationToken);
    }

    public Task<long> StartAsync(string deviceName, string outputPath, string bpfFilter, int durationSeconds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => NativeMethods.StartCapture(deviceName, outputPath, bpfFilter, durationSeconds), cancellationToken);
    }

    public Task<CaptureStatusModel> GetStatusAsync(long handle, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => ParseStatus(NativeMethods.GetCaptureStatusJson(handle)), cancellationToken);
    }

    public Task<CaptureStatusModel> StopAsync(long handle, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => ParseStatus(NativeMethods.StopCaptureJson(handle)), cancellationToken);
    }

    private static CaptureStatusModel ParseStatus(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var detections = new List<CaptureDetectionModel>();
        if (root.TryGetProperty("detections", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                detections.Add(new CaptureDetectionModel
                {
                    Code = item.TryGetProperty("code", out var code) ? code.GetString() ?? string.Empty : string.Empty,
                    Title = item.TryGetProperty("title", out var title) ? title.GetString() ?? string.Empty : string.Empty,
                    Description = item.TryGetProperty("description", out var description) ? description.GetString() ?? string.Empty : string.Empty,
                    Count = item.TryGetProperty("count", out var count) ? count.GetUInt64() : 0UL
                });
            }
        }

        return new CaptureStatusModel
        {
            Running = root.TryGetProperty("running", out var running) && running.GetBoolean(),
            Completed = root.TryGetProperty("completed", out var completed) && completed.GetBoolean(),
            OutputPath = root.TryGetProperty("outputPath", out var output) ? output.GetString() ?? string.Empty : string.Empty,
            BpfFilter = root.TryGetProperty("bpfFilter", out var filter) ? filter.GetString() ?? string.Empty : string.Empty,
            ErrorMessage = root.TryGetProperty("errorMessage", out var error) ? error.GetString() ?? string.Empty : string.Empty,
            PacketsSeen = root.TryGetProperty("packetsSeen", out var seen) ? seen.GetUInt64() : 0UL,
            PacketsWritten = root.TryGetProperty("packetsWritten", out var written) ? written.GetUInt64() : 0UL,
            BytesWritten = root.TryGetProperty("bytesWritten", out var bytes) ? bytes.GetUInt64() : 0UL,
            Detections = detections
        };
    }
}