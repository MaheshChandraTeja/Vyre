namespace Vyre.Maui.Models;

public sealed record AppSettings(
    string InterfaceSelection,
    int ScanIntervalSeconds,
    bool PrivacyHideBssids,
    bool PrivacyHideSsids
)
{
    public static AppSettings Default => new(
        InterfaceSelection: "auto",
        ScanIntervalSeconds: 10,
        PrivacyHideBssids: false,
        PrivacyHideSsids: false
    );
}
