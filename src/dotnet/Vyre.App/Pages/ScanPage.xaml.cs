using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Vyre.App.Models;
using Vyre.App.ViewModels;

namespace Vyre.App.Pages;

public partial class ScanPage : ContentPage
{
    private readonly ScanViewModel _viewModel;
    private bool _isNetworkDetailOpen;

    public ScanPage(ScanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnAccessPointTapped(object sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not AccessPointViewData accessPoint)
        {
            return;
        }

        await ShowNetworkDetailAsync(accessPoint);
    }

    private async void OnNetworkDetailBackdropTapped(object sender, TappedEventArgs e)
    {
        await CloseNetworkDetailAsync();
    }

    private async void OnCloseNetworkDetailClicked(object sender, EventArgs e)
    {
        await CloseNetworkDetailAsync();
    }

    private async Task ShowNetworkDetailAsync(AccessPointViewData accessPoint)
    {
        if (_isNetworkDetailOpen)
        {
            return;
        }

        _isNetworkDetailOpen = true;

        NetworkDetailCard.BindingContext = accessPoint;
        DetailHeadlineLabel.Text = BuildHeadline(accessPoint);
        SignalVerdictLabel.Text = BuildSignalVerdict(accessPoint.SignalDbm);
        SecurityVerdictLabel.Text = BuildSecurityVerdict(accessPoint.Security);
        ChannelVerdictLabel.Text = BuildChannelVerdict(accessPoint);

        var risk = GetRisk(accessPoint);
        DetailRiskLabel.Text = risk.Label;
        DetailRiskFrame.BackgroundColor = Color.FromArgb(risk.BackgroundHex);
        DetailRiskFrame.Stroke = new SolidColorBrush(Color.FromArgb(risk.BorderHex));

        NetworkDetailOverlay.IsVisible = true;
        NetworkDetailBackdrop.Opacity = 0;
        NetworkDetailCard.Opacity = 0;
        NetworkDetailCard.Scale = 0.94;
        NetworkDetailCard.TranslationY = 18;

        await Task.WhenAll(
            NetworkDetailBackdrop.FadeToAsync(1, 120, Easing.CubicOut),
            NetworkDetailCard.FadeToAsync(1, 160, Easing.CubicOut),
            NetworkDetailCard.ScaleToAsync(1, 180, Easing.CubicOut),
            NetworkDetailCard.TranslateToAsync(0, 0, 180, Easing.CubicOut));
    }

    private async Task CloseNetworkDetailAsync()
    {
        if (!_isNetworkDetailOpen)
        {
            return;
        }

        await Task.WhenAll(
            NetworkDetailBackdrop.FadeToAsync(0, 110, Easing.CubicIn),
            NetworkDetailCard.FadeToAsync(0, 110, Easing.CubicIn),
            NetworkDetailCard.ScaleToAsync(0.96, 110, Easing.CubicIn),
            NetworkDetailCard.TranslateToAsync(0, 14, 110, Easing.CubicIn));

        NetworkDetailOverlay.IsVisible = false;
        NetworkDetailCard.BindingContext = null;
        _isNetworkDetailOpen = false;
    }

    private static string BuildHeadline(AccessPointViewData accessPoint)
    {
        var ssid = Safe(accessPoint.Ssid, "this network");
        var risk = GetRisk(accessPoint).Label;
        var band = Safe(accessPoint.Band);
        var signal = accessPoint.SignalDbm == 0 ? "unknown signal" : $"{accessPoint.SignalDbm} dBm";

        return $"{ssid} is currently profiled as {risk.ToLowerInvariant()} on {band} with {signal}. Use this before joining random networks like a raccoon with a laptop.";
    }

    private static string BuildSignalVerdict(int signalDbm)
    {
        return signalDbm switch
        {
            0 => "Signal strength was not available from the platform scan. Some OS APIs ration Wi-Fi data like it is nuclear material.",
            >= -50 => "Excellent. This is close-range, high-quality signal territory. Throughput should be strong unless the router itself is tragic.",
            >= -60 => "Strong. Good for calls, streaming, and normal work without blaming the router every six minutes.",
            >= -67 => "Healthy. This should be fine for most real-world usage, including video calls and moderate traffic.",
            >= -75 => "Usable but not pretty. Expect occasional drops, jitter, or slow roaming if the environment is crowded.",
            _ => "Weak. Move closer, improve access point placement, or stop asking physics to apologize."
        };
    }

    private static string BuildSecurityVerdict(string? security)
    {
        var value = security ?? string.Empty;

        if (value.Contains("Open", StringComparison.OrdinalIgnoreCase))
        {
            return "Open network. Treat it as hostile for sensitive traffic. Use VPN, avoid private logins, and do not trust captive portals just because they look friendly.";
        }

        if (value.Contains("WEP", StringComparison.OrdinalIgnoreCase))
        {
            return "WEP detected. This is legacy security and should be retired. It belongs in a museum next to floppy disks and bad decisions.";
        }

        if (value.Contains("WPA3", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("SAE", StringComparison.OrdinalIgnoreCase))
        {
            return "WPA3-class security detected. This is the preferred modern posture when device compatibility allows it.";
        }

        if (value.Contains("WPA2", StringComparison.OrdinalIgnoreCase))
        {
            return "WPA2 detected. Still acceptable for most environments, but WPA3 is the better long-term target.";
        }

        if (value.Contains("OWE", StringComparison.OrdinalIgnoreCase))
        {
            return "OWE detected. Better than classic open Wi-Fi because traffic can be encrypted, but identity/trust still needs caution.";
        }

        if (string.IsNullOrWhiteSpace(value) ||
            value.Contains("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return "Security could not be confidently identified. Verify router settings before trusting this network.";
        }

        return "Security is present, but the platform returned a non-standard label. Validate the AP configuration if this network matters.";
    }

    private static string BuildChannelVerdict(AccessPointViewData accessPoint)
    {
        var band = accessPoint.Band ?? string.Empty;
        var channel = accessPoint.Channel;
        var frequency = accessPoint.FrequencyMhz;

        if (band.Contains('6'))
        {
            return $"6 GHz on channel {channel}. Low congestion and high performance potential, assuming your device and router both support it.";
        }

        if (band.Contains('5'))
        {
            return $"5 GHz on channel {channel}. Usually faster and cleaner than 2.4 GHz, but range falls off faster through walls.";
        }

        if (band.Contains("2.4", StringComparison.OrdinalIgnoreCase))
        {
            var channelHint = channel is 1 or 6 or 11
                ? "This is one of the usual non-overlapping 2.4 GHz channels. Sensible. Disturbingly rare."
                : "This may overlap nearby 2.4 GHz traffic. Channels 1, 6, and 11 are usually cleaner choices.";

            return $"2.4 GHz on channel {channel}. {channelHint}";
        }

        if (frequency > 0)
        {
            return $"The platform reported {frequency} MHz but not a reliable band. Vyre can still show it, because apparently we do the OS's homework too.";
        }

        return "Channel and frequency were not available from this scan result.";
    }

    private static (string Label, string BackgroundHex, string BorderHex) GetRisk(AccessPointViewData accessPoint)
    {
        var security = accessPoint.Security ?? string.Empty;

        if (security.Contains("Open", StringComparison.OrdinalIgnoreCase) ||
            security.Contains("WEP", StringComparison.OrdinalIgnoreCase))
        {
            return ("High risk", "#7F1D1D", "#EF4444");
        }

        if (accessPoint.SignalDbm != 0 && accessPoint.SignalDbm <= -75)
        {
            return ("Weak link", "#713F12", "#F59E0B");
        }

        if (security.Contains("Unknown", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(security))
        {
            return ("Review", "#1E3A8A", "#60A5FA");
        }

        if (security.Contains("WPA3", StringComparison.OrdinalIgnoreCase) ||
            security.Contains("SAE", StringComparison.OrdinalIgnoreCase))
        {
            return ("Strong", "#064E3B", "#34D399");
        }

        return ("Normal", "#1E293B", "#64748B");
    }

    private static string Safe(string? value, string fallback = "Unknown")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}