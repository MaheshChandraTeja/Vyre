namespace Vyre.App.Models;

public sealed class AppSettings
{
    public string SelectedInterface { get; set; } = "Auto";
    public int ScanIntervalSeconds { get; set; } = 10;
    public bool PrivacyModeEnabled { get; set; } = true;
    public bool SaveReportHistory { get; set; } = true;
    public bool ShareDiagnostics { get; set; }
}
