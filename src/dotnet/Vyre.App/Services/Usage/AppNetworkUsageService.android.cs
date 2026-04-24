#if ANDROID
using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Vyre.App.Models;

namespace Vyre.App.Services.Usage;

public sealed partial class AppNetworkUsageService
{
    private static partial bool GetShouldShowTab() => true;

    private static partial Task OpenAccessSettingsCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var intent = new Intent(Android.Provider.Settings.ActionUsageAccessSettings);
        intent.AddFlags(ActivityFlags.NewTask);
        Android.App.Application.Context.StartActivity(intent);
        return Task.CompletedTask;
    }

    private static partial Task<bool> EnsureAccessCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(HasUsageAccess());
    }

    private static partial Task<AppNetworkUsageSnapshot> GetUsageCoreAsync(
        UsageNetworkScope networkScope,
        UsageTimeRange timeRange,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            if (!HasUsageAccess())
            {
                return new AppNetworkUsageSnapshot
                {
                    PlatformName = "Android",
                    CurrentNetworkType = GetCurrentNetworkTypeLabel(),
                    IsSupported = false,
                    AvailabilityMessage = "Usage access is not granted. Open settings and allow Usage Access for Vyre to view per-app network usage.",
                    Items = Array.Empty<AppNetworkUsageModel>()
                };
            }

            var context = Android.App.Application.Context;
            var statsManager = context.GetSystemService(Context.NetworkStatsService) as NetworkStatsManager;
            var packageManager = context.PackageManager;

            if (statsManager is null || packageManager is null)
            {
                return new AppNetworkUsageSnapshot
                {
                    PlatformName = "Android",
                    CurrentNetworkType = GetCurrentNetworkTypeLabel(),
                    IsSupported = false,
                    AvailabilityMessage = "Android network usage service is unavailable on this device.",
                    Items = Array.Empty<AppNetworkUsageModel>()
                };
            }

            var range = ResolveRange(timeRange);
            var startMs = range.Start.ToUnixTimeMilliseconds();
            var endMs = range.End.ToUnixTimeMilliseconds();

            var items = new List<AppNetworkUsageModel>();
            foreach (var transport in ResolveAndroidNetworkTypes(networkScope))
            {
                cancellationToken.ThrowIfCancellationRequested();

                NetworkStats? stats = null;
                try
                {
                    stats = statsManager.QuerySummary(transport, null, startMs, endMs);
                    if (stats is null)
                    {
                        continue;
                    }

                    var bucket = new NetworkStats.Bucket();

                    while (stats.HasNextBucket)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        stats.GetNextBucket(bucket);

                        if (bucket.Uid is <= 0)
                        {
                            continue;
                        }

                        if (bucket.RxBytes <= 0 && bucket.TxBytes <= 0)
                        {
                            continue;
                        }

                        var packageNames = packageManager.GetPackagesForUid(bucket.Uid);
                        if (packageNames is null || packageNames.Length == 0)
                        {
                            continue;
                        }

                        var packageId = packageNames[0];
                        var appName = ResolveAppName(packageManager, packageId);

                        var existingIndex = items.FindIndex(x =>
                            string.Equals(x.PackageId, packageId, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(x.NetworkType, ToScopeLabel(transport), StringComparison.OrdinalIgnoreCase));

                        if (existingIndex >= 0)
                        {
                            var existing = items[existingIndex];
                            items[existingIndex] = new AppNetworkUsageModel
                            {
                                AppName = existing.AppName,
                                PackageId = existing.PackageId,
                                UploadBytes = existing.UploadBytes + bucket.TxBytes,
                                DownloadBytes = existing.DownloadBytes + bucket.RxBytes,
                                LastActiveLabel = RangeLabel(timeRange),
                                NetworkType = existing.NetworkType
                            };
                        }
                        else
                        {
                            items.Add(new AppNetworkUsageModel
                            {
                                AppName = appName,
                                PackageId = packageId,
                                UploadBytes = bucket.TxBytes,
                                DownloadBytes = bucket.RxBytes,
                                LastActiveLabel = RangeLabel(timeRange),
                                NetworkType = ToScopeLabel(transport)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new AppNetworkUsageSnapshot
                    {
                        PlatformName = "Android",
                        CurrentNetworkType = GetCurrentNetworkTypeLabel(),
                        IsSupported = false,
                        AvailabilityMessage = $"Failed to query Android usage stats: {ex.Message}",
                        Items = Array.Empty<AppNetworkUsageModel>()
                    };
                }
                finally
                {
                    stats?.Close();
                }
            }

            var normalized = items
                .GroupBy(x => x.PackageId, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var first = group.First();
                    return new AppNetworkUsageModel
                    {
                        AppName = first.AppName,
                        PackageId = first.PackageId,
                        UploadBytes = group.Sum(x => x.UploadBytes),
                        DownloadBytes = group.Sum(x => x.DownloadBytes),
                        LastActiveLabel = RangeLabel(timeRange),
                        NetworkType = networkScope switch
                        {
                            UsageNetworkScope.Wifi => "Wi-Fi",
                            UsageNetworkScope.Mobile => "Mobile",
                            _ => "All"
                        }
                    };
                })
                .OrderByDescending(x => x.TotalBytes)
                .ToList();

            return new AppNetworkUsageSnapshot
            {
                PlatformName = "Android",
                CurrentNetworkType = GetCurrentNetworkTypeLabel(),
                IsSupported = true,
                AvailabilityMessage = normalized.Count == 0
                    ? "No per-app network usage was reported for the selected period."
                    : "Android usage data is sourced from NetworkStatsManager.",
                Items = normalized
            };
        }, cancellationToken);
    }

    private static bool HasUsageAccess()
    {
        var context = Android.App.Application.Context;
        var appOps = context.GetSystemService(Context.AppOpsService) as AppOpsManager;
        if (appOps is null)
        {
            return false;
        }

        var packageName = context.PackageName;
        if (string.IsNullOrWhiteSpace(packageName))
        {
            return false;
        }

        var mode = appOps.CheckOpNoThrow(
            AppOpsManager.OpstrGetUsageStats,
            Process.MyUid(),
            packageName);

        return mode == AppOpsManagerMode.Allowed;
    }

    private static IEnumerable<ConnectivityType> ResolveAndroidNetworkTypes(UsageNetworkScope scope)
    {
        return scope switch
        {
            UsageNetworkScope.Wifi => [ConnectivityType.Wifi],
            UsageNetworkScope.Mobile => [ConnectivityType.Mobile],
            _ => [ConnectivityType.Wifi, ConnectivityType.Mobile]
        };
    }

    private static string ResolveAppName(PackageManager packageManager, string packageId)
    {
        try
        {
            var appInfo = packageManager.GetApplicationInfo(packageId, 0);
            return packageManager.GetApplicationLabel(appInfo)?.ToString() ?? packageId;
        }
        catch
        {
            return packageId;
        }
    }

    private static string ToScopeLabel(ConnectivityType networkType) =>
        networkType == ConnectivityType.Wifi ? "Wi-Fi" : "Mobile";

    private static string RangeLabel(UsageTimeRange range) =>
        range switch
        {
            UsageTimeRange.Last7Days => "Last 7d",
            UsageTimeRange.Last30Days => "Last 30d",
            _ => "Last 24h"
        };

    private static string GetCurrentNetworkTypeLabel()
    {
        var access = Connectivity.Current.NetworkAccess;
        if (access != NetworkAccess.Internet)
        {
            return "Offline";
        }

        var profiles = Connectivity.Current.ConnectionProfiles;
        if (profiles.Contains(ConnectionProfile.WiFi))
        {
            return "Wi-Fi";
        }

        if (profiles.Contains(ConnectionProfile.Cellular))
        {
            return "Mobile";
        }

        return "Other";
    }
}
#endif
