namespace Vyre.App.Models;

public sealed record NativeAccessPoint(
    string Bssid,
    string Ssid,
    int Channel,
    int RssiDbm,
    int FrequencyMhz,
    string Security,
    bool Hidden);
