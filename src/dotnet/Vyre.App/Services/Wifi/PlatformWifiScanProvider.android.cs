#if ANDROID
using Android.Content;
using Android.Locations;
using Android.Net.Wifi;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using Vyre.App.Models;

namespace Vyre.App.Services.Wifi;

public sealed partial class PlatformWifiScanProvider
{
    private static async partial Task<PlatformWifiScanPayload> ScanCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permissionState = await EnsureAndroidPermissionsAsync();
        if (!permissionState.IsGranted)
        {
            throw new InvalidOperationException(permissionState.Message);
        }

        var context = Android.App.Application.Context;
        var wifiManager = context.GetSystemService(Context.WifiService) as WifiManager;
        if (wifiManager is null)
        {
            throw new InvalidOperationException("Android Wi-Fi service is unavailable on this device.");
        }

        if (!wifiManager.IsWifiEnabled)
        {
            throw new InvalidOperationException("Wi-Fi is disabled. Enable Wi-Fi and try again.");
        }

        try
        {
#pragma warning disable CS0618
            _ = wifiManager.StartScan();
#pragma warning restore CS0618
        }
        catch
        {
            // Starting a fresh scan is best effort. Android increasingly rate-limits this.
            // We still read the latest allowed scan results.
        }

#pragma warning disable CS0618
        var nativeResults = wifiManager.ScanResults;
#pragma warning restore CS0618

        var accessPoints = new List<AccessPointViewData>();

        if (nativeResults is not null)
        {
            foreach (var item in nativeResults)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var ssid = string.IsNullOrWhiteSpace(item.Ssid) ? "<Hidden>" : item.Ssid;
                var security = ParseSecurity(item.Capabilities);
                var frequencyMhz = item.Frequency;
                var channel = FrequencyToChannel(frequencyMhz);
                var band = FrequencyToBand(frequencyMhz);

                accessPoints.Add(new AccessPointViewData
                {
                    Ssid = ssid,
                    Bssid = item.Bssid ?? string.Empty,
                    Band = band,
                    Channel = channel,
                    FrequencyMhz = frequencyMhz,
                    SignalDbm = item.Level,
                    Security = security
                });
            }
        }

        accessPoints = accessPoints
            .OrderByDescending(x => x.SignalDbm)
            .ThenBy(x => x.Ssid, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PlatformWifiScanPayload
        {
            SourcePlatform = "Android",
            IsPartial = false,
            CapabilityMessage = "Android scan uses platform Wi-Fi APIs within OS permission and rate-limit rules.",
            CurrentSsid = TryGetCurrentSsid(wifiManager),
            Warnings = BuildWarnings(accessPoints),
            AccessPoints = accessPoints
        };
    }

    private static partial Task<string> GetCapabilitySummaryCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            "Android: full AP scanning is supported where Wi-Fi and location permissions are granted and OS policy allows scan results.");
    }

    private static string ParseSecurity(string? capabilities)
    {
        if (string.IsNullOrWhiteSpace(capabilities))
        {
            return "Unknown";
        }

        var value = capabilities.ToUpperInvariant();

        if (value.Contains("SAE"))
        {
            return "WPA3-Personal";
        }

        if (value.Contains("WPA3"))
        {
            return "WPA3";
        }

        if (value.Contains("OWE"))
        {
            return "OWE";
        }

        if (value.Contains("WPA2"))
        {
            return "WPA2";
        }

        if (value.Contains("WPA"))
        {
            return "WPA";
        }

        if (value.Contains("WEP"))
        {
            return "WEP";
        }

        return "[ESS]".Equals(value, StringComparison.OrdinalIgnoreCase) ? "Open" : capabilities;
    }

    private static List<string> BuildWarnings(List<AccessPointViewData> accessPoints)
    {
        var warnings = new List<string>();

        if (accessPoints.Count == 0)
        {
            warnings.Add("Android returned no visible access points. This can happen due to OS scan throttling, disabled location services, or no nearby networks.");
        }

        if (accessPoints.Any(x => x.IsOpen))
        {
            warnings.Add("One or more open networks were detected.");
        }

        if (accessPoints.Any(x => x.FrequencyMhz <= 0))
        {
            warnings.Add("Some Android scan results did not include frequency data. Band labels may be less reliable for those rows.");
        }

        return warnings;
    }

    private static string? TryGetCurrentSsid(WifiManager wifiManager)
    {
#pragma warning disable CS0618
        var info = wifiManager.ConnectionInfo;
#pragma warning restore CS0618

        if (info is null)
        {
            return null;
        }

        var ssid = info.SSID;
        if (string.IsNullOrWhiteSpace(ssid) || ssid == "<unknown ssid>")
        {
            return null;
        }

        return ssid.Trim('"');
    }

    private static async Task<(bool IsGranted, string Message)> EnsureAndroidPermissionsAsync()
    {
        var locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (locationStatus != PermissionStatus.Granted)
        {
            locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (OperatingSystem.IsAndroidVersionAtLeast((int)BuildVersionCodes.Tiramisu))
        {
            var wifiStatus = await Permissions.CheckStatusAsync<NearbyWifiDevicesPermission>();
            if (wifiStatus != PermissionStatus.Granted)
            {
                wifiStatus = await Permissions.RequestAsync<NearbyWifiDevicesPermission>();
            }

            if (wifiStatus != PermissionStatus.Granted)
            {
                return (false, "Nearby Wi-Fi Devices permission was denied. Android will not return scan results without it.");
            }
        }

        if (locationStatus != PermissionStatus.Granted)
        {
            return (false, "Location permission was denied. Android Wi-Fi scanning requires it on most versions.");
        }

        var locationManager = Android.App.Application.Context.GetSystemService(Context.LocationService) as LocationManager;
        var locationEnabled = locationManager?.IsProviderEnabled(LocationManager.GpsProvider) == true
            || locationManager?.IsProviderEnabled(LocationManager.NetworkProvider) == true;

        if (!locationEnabled)
        {
            return (false, "Location services are turned off. Android usually hides Wi-Fi scan results until location services are enabled.");
        }

        return (true, string.Empty);
    }

    private sealed class NearbyWifiDevicesPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            new[]
            {
                ("android.permission.NEARBY_WIFI_DEVICES", true)
            };
    }
}
#endif
