using Vyre.Maui.Services;

namespace Vyre.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        try
        {
            var version = WifiNative.GetVersion();

            var scanJson =
                "{" +
                "\"report_id\":\"m2-001\"," +
                "\"generated_at_utc\":\"2026-02-01T12:00:00Z\"," +
                "\"captured_at_utc\":\"2026-02-01T11:59:00Z\"," +
                "\"platform\":\"windows\"," +
                "\"app_version\":\"0.1.0\"," +
                "\"access_points\":[" +
                  "{" +
                    "\"bssid\":\"11:11:11:11:11:11\"," +
                    "\"ssid\":\"HomeWiFi\"," +
                    "\"band\":\"2.4ghz\"," +
                    "\"channel\":6," +
                    "\"rssi_dbm\":-43," +
                    "\"security\":\"WPA2\"" +
                  "}," +
                  "{" +
                    "\"bssid\":\"22:22:22:22:22:22\"," +
                    "\"ssid\":\"CafeNet\"," +
                    "\"band\":\"5ghz\"," +
                    "\"channel\":36," +
                    "\"rssi_dbm\":-61," +
                    "\"security\":\"WPA2\"" +
                  "}" +
                "]" +
                "}";

            var reportJson = WifiNative.AnalyzeJson(scanJson);

            System.Diagnostics.Debug.WriteLine($"wifi-core version: {version}");
            System.Diagnostics.Debug.WriteLine($"report: {reportJson}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Native call failed: {ex}");
        }
    }
}
