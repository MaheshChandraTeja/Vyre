namespace Vyre.App.Models;

public sealed class AppNetworkUsageModel
{
    public string AppName { get; init; } = string.Empty;
    public string PackageId { get; init; } = string.Empty;
    public long UploadBytes { get; init; }
    public long DownloadBytes { get; init; }
    public long TotalBytes => UploadBytes + DownloadBytes;
    public string LastActiveLabel { get; init; } = string.Empty;
    public string NetworkType { get; init; } = string.Empty;
}

public sealed class AppNetworkUsageSnapshot
{
    public string PlatformName { get; init; } = string.Empty;
    public string CurrentNetworkType { get; init; } = string.Empty;
    public string AvailabilityMessage { get; init; } = string.Empty;
    public bool IsSupported { get; init; }
    public IReadOnlyList<AppNetworkUsageModel> Items { get; init; } = Array.Empty<AppNetworkUsageModel>();

    public long TotalUploadBytes => Items.Sum(x => x.UploadBytes);
    public long TotalDownloadBytes => Items.Sum(x => x.DownloadBytes);
    public long TotalBytes => TotalUploadBytes + TotalDownloadBytes;

    public IReadOnlyList<AppNetworkUsageModel> TopTalkers =>
        Items.OrderByDescending(x => x.TotalBytes).Take(3).ToList();
}

public enum UsageNetworkScope
{
    All = 0,
    Wifi = 1,
    Mobile = 2
}

public enum UsageTimeRange
{
    Last24Hours = 0,
    Last7Days = 1,
    Last30Days = 2
}

public enum UsageSortMode
{
    Total = 0,
    Upload = 1,
    Download = 2
}