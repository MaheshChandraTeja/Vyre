namespace Vyre.Maui.Models;

public sealed record AccessPointModel(
    string Bssid,
    string Ssid,
    string Band,
    int? Channel,
    int? RssiDbm,
    string? Security
);
