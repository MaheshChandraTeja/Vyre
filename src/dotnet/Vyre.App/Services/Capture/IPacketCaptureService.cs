using Vyre.App.Models;

namespace Vyre.App.Services.Capture;

public interface IPacketCaptureService
{
    Task<IReadOnlyList<CaptureDeviceModel>> ListDevicesAsync(CancellationToken cancellationToken);
    Task<long> StartAsync(string deviceName, string outputPath, string bpfFilter, int durationSeconds, CancellationToken cancellationToken);
    Task<CaptureStatusModel> GetStatusAsync(long handle, CancellationToken cancellationToken);
    Task<CaptureStatusModel> StopAsync(long handle, CancellationToken cancellationToken);
}