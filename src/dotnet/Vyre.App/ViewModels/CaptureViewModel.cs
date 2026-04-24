using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Vyre.App.Models;
using Vyre.App.Services.Capture;

namespace Vyre.App.ViewModels;

public sealed partial class CaptureViewModel : BaseViewModel, IDisposable
{
    private readonly IPacketCaptureService _packetCaptureService;
    private long _currentHandle;
    private CancellationTokenSource? _pollCts;
    private DateTimeOffset? _captureStartedUtc;
    private ulong _lastPacketsSeen;
    private DateTimeOffset? _lastRateSampleUtc;

    public ObservableCollection<CaptureDeviceModel> Devices { get; } = new();
    public ObservableCollection<CaptureDetectionModel> Detections { get; } = new();

    public IReadOnlyList<CaptureFilterPreset> FilterPresets { get; } =
    [
        new CaptureFilterPreset(
            "All traffic",
            string.Empty,
            "Capture all packets allowed by the selected interface and platform."),

        new CaptureFilterPreset(
            "ARP discovery",
            "arp",
            "Watch local address resolution traffic. Useful for device discovery and LAN mapping."),

        new CaptureFilterPreset(
            "DNS activity",
            "udp port 53",
            "Capture DNS queries and responses. Good for spotting noisy apps and lookup behavior."),

        new CaptureFilterPreset(
            "DHCP lease traffic",
            "udp port 67 or udp port 68",
            "Capture DHCP negotiation traffic from clients and routers."),

        new CaptureFilterPreset(
            "LAN essentials",
            "arp or udp port 53 or udp port 67 or udp port 68",
            "Balanced preset for ARP, DNS, and DHCP. Usually the best starting point.")
    ];

    [ObservableProperty] private CaptureDeviceModel? selectedDevice;
    [ObservableProperty] private CaptureFilterPreset? selectedPreset;
    [ObservableProperty] private int durationSeconds = 30;
    [ObservableProperty] private string statusText = "Ready to capture.";
    [ObservableProperty] private string outputPath = "No capture file created yet.";
    [ObservableProperty] private ulong packetsSeen;
    [ObservableProperty] private ulong packetsWritten;
    [ObservableProperty] private ulong bytesWritten;
    [ObservableProperty] private bool canStart = true;
    [ObservableProperty] private bool canStop;
    [ObservableProperty] private bool hasOutput;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private bool hasDetections;
    [ObservableProperty] private bool hasNoDetections = true;
    [ObservableProperty] private double captureProgress;
    [ObservableProperty] private string elapsedText = "00:00";
    [ObservableProperty] private string packetRateText = "0/s";
    [ObservableProperty] private string bytesWrittenText = "0 B";
    [ObservableProperty] private string captureStateLabel = "READY";
    [ObservableProperty] private string captureModeText = "Managed packet capture with bounded duration and local PCAPNG output.";
    [ObservableProperty] private string outputSummaryText = "No output yet. Start a capture to generate a local PCAPNG file.";
    [ObservableProperty] private string detectionSummaryText = "No detections have been produced for this session.";
    [ObservableProperty] private string detectionCountText = "0";

    public string SelectedPresetDescription =>
        SelectedPreset?.Summary ?? "Choose a capture preset.";

    public string DurationLabel =>
        DurationSeconds < 60
            ? $"{DurationSeconds} second bounded capture"
            : $"{DurationSeconds / 60.0:0.#} minute bounded capture";

    public IAsyncRelayCommand RefreshDevicesCommand { get; }
    public IAsyncRelayCommand StartCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand CopyOutputPathCommand { get; }
    public IAsyncRelayCommand ShareCaptureCommand { get; }

    public IRelayCommand SetDuration15Command { get; }
    public IRelayCommand SetDuration30Command { get; }
    public IRelayCommand SetDuration60Command { get; }
    public IRelayCommand SetDuration300Command { get; }

    public CaptureViewModel(IPacketCaptureService packetCaptureService)
    {
        _packetCaptureService = packetCaptureService;

        Title = "Capture";
        SelectedPreset = FilterPresets.FirstOrDefault(p => p.Expression == "arp or udp port 53 or udp port 67 or udp port 68")
                         ?? (FilterPresets.Count > 0 ? FilterPresets[0] : null);

        RefreshDevicesCommand = new AsyncRelayCommand(LoadDevicesAsync);
        StartCommand = new AsyncRelayCommand(StartAsync, () => CanStart);
        StopCommand = new AsyncRelayCommand(StopAsync, () => CanStop);
        CopyOutputPathCommand = new AsyncRelayCommand(CopyOutputPathAsync, () => HasOutput);
        ShareCaptureCommand = new AsyncRelayCommand(ShareCaptureAsync, () => HasOutput);

        SetDuration15Command = new RelayCommand(() => SetDuration(15));
        SetDuration30Command = new RelayCommand(() => SetDuration(30));
        SetDuration60Command = new RelayCommand(() => SetDuration(60));
        SetDuration300Command = new RelayCommand(() => SetDuration(300));
    }

    public async Task InitializeAsync()
    {
        if (Devices.Count == 0)
        {
            await LoadDevicesAsync();
        }
    }

    private async Task LoadDevicesAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            var devices = await _packetCaptureService.ListDevicesAsync(cts.Token);

            Devices.Clear();

            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            SelectedDevice ??= Devices.FirstOrDefault();

            StatusText = Devices.Count == 0
                ? "No capture devices available on this platform."
                : $"{Devices.Count} capture device(s) available.";

            CaptureModeText = Devices.Count == 0
                ? "Capture is unavailable here. Check platform permissions, adapter support, or driver availability."
                : "Select an interface and run a bounded local capture. Output stays on this device.";
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
            StatusText = "Device refresh failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StartAsync()
    {
        if (SelectedDevice is null)
        {
            SetError("Select a capture device first.");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            ResetSessionCounters();

            var safeDuration = Math.Clamp(DurationSeconds, 1, 3600);
            DurationSeconds = safeDuration;

            var fileName = $"vyre-capture-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.pcapng";
            var captureDir = Path.Combine(FileSystem.Current.AppDataDirectory, "captures");
            Directory.CreateDirectory(captureDir);

            var path = Path.Combine(captureDir, fileName);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            _currentHandle = await _packetCaptureService.StartAsync(
                SelectedDevice.Name,
                path,
                SelectedPreset?.Expression ?? string.Empty,
                safeDuration,
                cts.Token);

            OutputPath = path;
            HasOutput = true;
            OutputSummaryText = "Capture file is being written locally.";
            StatusText = "Capture running...";
            CaptureStateLabel = "LIVE";
            CaptureModeText = $"{SelectedPreset?.Name ?? "Custom"} capture running for {DurationLabel.ToLowerInvariant()}.";

            _captureStartedUtc = DateTimeOffset.UtcNow;
            _lastPacketsSeen = 0;
            _lastRateSampleUtc = _captureStartedUtc;

            CanStart = false;
            CanStop = true;

            NotifyCommandStates();

            _pollCts?.Cancel();
            _pollCts?.Dispose();
            _pollCts = new CancellationTokenSource();

            _ = PollAsync(_pollCts.Token);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
            StatusText = "Capture failed to start.";
            CaptureStateLabel = "ERROR";
            CanStart = true;
            CanStop = false;
            NotifyCommandStates();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StopAsync()
    {
        if (_currentHandle == 0)
        {
            return;
        }

        try
        {
            _pollCts?.Cancel();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var status = await _packetCaptureService.StopAsync(_currentHandle, cts.Token);

            ApplyStatus(status);

            StatusText = string.IsNullOrWhiteSpace(status.ErrorMessage)
                ? "Capture completed."
                : status.ErrorMessage;

            CaptureStateLabel = string.IsNullOrWhiteSpace(status.ErrorMessage) ? "SAVED" : "WARNING";
            OutputSummaryText = HasOutput
                ? $"Saved capture • {BytesWrittenText} • {PacketsWritten} packets written"
                : "Capture stopped without an output file.";
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
            StatusText = "Capture stop failed.";
            CaptureStateLabel = "ERROR";
        }
        finally
        {
            _currentHandle = 0;
            CanStart = true;
            CanStop = false;
            NotifyCommandStates();
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _currentHandle != 0)
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var status = await _packetCaptureService.GetStatusAsync(_currentHandle, linked.Token);

                ApplyStatus(status);

                if (status.Completed || !status.Running)
                {
                    _currentHandle = 0;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CanStart = true;
                        CanStop = false;
                        CaptureStateLabel = string.IsNullOrWhiteSpace(status.ErrorMessage) ? "SAVED" : "WARNING";
                        StatusText = string.IsNullOrWhiteSpace(status.ErrorMessage)
                            ? "Capture completed."
                            : status.ErrorMessage;
                        OutputSummaryText = HasOutput
                            ? $"Saved capture • {BytesWrittenText} • {PacketsWritten} packets written"
                            : "Capture completed without a saved output file.";

                        NotifyCommandStates();
                    });

                    break;
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SetError(ex.Message);
                CaptureStateLabel = "ERROR";
                StatusText = "Capture status polling failed.";
            });
        }
    }

    private void ApplyStatus(CaptureStatusModel status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var now = DateTimeOffset.UtcNow;

            PacketsSeen = status.PacketsSeen;
            PacketsWritten = status.PacketsWritten;
            BytesWritten = status.BytesWritten;
            BytesWrittenText = FormatBytes(status.BytesWritten);

            if (!string.IsNullOrWhiteSpace(status.OutputPath))
            {
                OutputPath = status.OutputPath;
                HasOutput = true;
            }

            UpdateElapsedAndProgress(now);
            UpdatePacketRate(now, status.PacketsSeen);

            Detections.Clear();

            foreach (var detection in status.Detections)
            {
                Detections.Add(detection);
            }

            HasDetections = Detections.Count > 0;
            HasNoDetections = !HasDetections;
            DetectionCountText = Detections.Count.ToString(CultureInfo.InvariantCulture);
            DetectionSummaryText = HasDetections
                ? $"{Detections.Count} detection(s) surfaced from this capture."
                : "No detections surfaced yet. Clean traffic is allowed to be boring.";

            if (!string.IsNullOrWhiteSpace(status.ErrorMessage))
            {
                SetError(status.ErrorMessage);
                StatusText = status.ErrorMessage;
                CaptureStateLabel = "WARNING";
            }
            else if (status.Running)
            {
                StatusText = $"Capture running • {status.PacketsSeen} packets seen";
                CaptureStateLabel = "LIVE";
                OutputSummaryText = $"Writing PCAPNG locally • {BytesWrittenText}";
            }
            else if (status.Completed)
            {
                StatusText = "Capture completed.";
                CaptureStateLabel = "SAVED";
                OutputSummaryText = $"Saved capture • {BytesWrittenText} • {PacketsWritten} packets written";
            }
        });
    }

    private async Task CopyOutputPathAsync()
    {
        if (!HasOutput || string.IsNullOrWhiteSpace(OutputPath))
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(OutputPath);
        StatusText = "Output path copied.";
    }

    private async Task ShareCaptureAsync()
    {
        if (!HasOutput || string.IsNullOrWhiteSpace(OutputPath) || !File.Exists(OutputPath))
        {
            SetError("Capture file is not available to share yet.");
            return;
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Vyre capture",
            File = new ShareFile(OutputPath)
        });
    }

    private void SetDuration(int seconds)
    {
        DurationSeconds = Math.Clamp(seconds, 1, 3600);
    }

    private void ResetSessionCounters()
    {
        PacketsSeen = 0;
        PacketsWritten = 0;
        BytesWritten = 0;
        BytesWrittenText = "0 B";
        PacketRateText = "0/s";
        ElapsedText = "00:00";
        CaptureProgress = 0;
        Detections.Clear();
        HasDetections = false;
        HasNoDetections = true;
        DetectionCountText = "0";
        DetectionSummaryText = "No detections have been produced for this session.";
    }

    private void UpdateElapsedAndProgress(DateTimeOffset now)
    {
        if (_captureStartedUtc is null)
        {
            ElapsedText = "00:00";
            CaptureProgress = 0;
            return;
        }

        var elapsed = now - _captureStartedUtc.Value;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        ElapsedText = elapsed.TotalHours >= 1
            ? elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
            : elapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture);

        CaptureProgress = DurationSeconds <= 0
            ? 0
            : Math.Clamp(elapsed.TotalSeconds / DurationSeconds, 0, 1);
    }

    private void UpdatePacketRate(DateTimeOffset now, ulong currentPacketsSeen)
    {
        if (_lastRateSampleUtc is null)
        {
            _lastRateSampleUtc = now;
            _lastPacketsSeen = currentPacketsSeen;
            PacketRateText = "0/s";
            return;
        }

        var seconds = Math.Max(1, (now - _lastRateSampleUtc.Value).TotalSeconds);
        var delta = currentPacketsSeen >= _lastPacketsSeen
            ? currentPacketsSeen - _lastPacketsSeen
            : 0;

        var rate = delta / seconds;
        PacketRateText = $"{rate:0.#}/s";

        _lastPacketsSeen = currentPacketsSeen;
        _lastRateSampleUtc = now;
    }

    private static string FormatBytes(ulong bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{value:0} {units[unitIndex]}"
            : $"{value:0.##} {units[unitIndex]}";
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = !string.IsNullOrWhiteSpace(message);
    }

    private void ClearError()
    {
        ErrorMessage = null;
        HasError = false;
    }

    private void NotifyCommandStates()
    {
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        CopyOutputPathCommand.NotifyCanExecuteChanged();
        ShareCaptureCommand.NotifyCanExecuteChanged();
    }

    partial void OnCanStartChanged(bool value)
    {
        StartCommand?.NotifyCanExecuteChanged();
    }

    partial void OnCanStopChanged(bool value)
    {
        StopCommand?.NotifyCanExecuteChanged();
    }

    partial void OnHasOutputChanged(bool value)
    {
        CopyOutputPathCommand?.NotifyCanExecuteChanged();
        ShareCaptureCommand?.NotifyCanExecuteChanged();
    }

    partial void OnDurationSecondsChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 3600);

        if (clamped != value)
        {
            DurationSeconds = clamped;
            return;
        }

        OnPropertyChanged(nameof(DurationLabel));
    }

    partial void OnSelectedPresetChanged(CaptureFilterPreset? value)
    {
        OnPropertyChanged(nameof(SelectedPresetDescription));

        CaptureModeText = value is null
            ? "Managed packet capture with bounded duration and local PCAPNG output."
            : $"{value.Name}: {value.Summary}";
    }

    public void Dispose()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
    }
}

public sealed record CaptureFilterPreset(
    string Name,
    string Expression,
    string Summary);
