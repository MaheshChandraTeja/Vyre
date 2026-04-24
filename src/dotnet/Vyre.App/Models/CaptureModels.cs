namespace Vyre.App.Models;

public sealed class CaptureDeviceModel
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public override string ToString() => string.IsNullOrWhiteSpace(Description) ? Name : $"{Description} ({Name})";
}

public sealed class CaptureDetectionModel
{
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ulong Count { get; init; }
}

public sealed class CaptureStatusModel
{
    public bool Running { get; init; }
    public bool Completed { get; init; }
    public string OutputPath { get; init; } = string.Empty;
    public string BpfFilter { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public ulong PacketsSeen { get; init; }
    public ulong PacketsWritten { get; init; }
    public ulong BytesWritten { get; init; }
    public IReadOnlyList<CaptureDetectionModel> Detections { get; init; } = Array.Empty<CaptureDetectionModel>();
}