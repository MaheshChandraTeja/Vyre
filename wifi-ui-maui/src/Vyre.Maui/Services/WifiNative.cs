using System;
using System.Runtime.InteropServices;

namespace Vyre.Maui.Services;

internal static class WifiNative
{
    private const string DllName = "wifi-interop";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr vyre_wifi_last_error_message();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void vyre_wifi_free(IntPtr p);

    private static string GetLastError()
    {
        var ptr = vyre_wifi_last_error_message();
        if (ptr == IntPtr.Zero) return "Unknown native error";
        return Marshal.PtrToStringAnsi(ptr) ?? "Unknown native error";
    }

    private static string TakeUtf8StringAndFree(IntPtr p)
    {
        if (p == IntPtr.Zero) return string.Empty;
        try
        {
            return Marshal.PtrToStringAnsi(p) ?? string.Empty;
        }
        finally
        {
            vyre_wifi_free(p);
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_wifi_get_version(out IntPtr out_utf8);

    public static string GetVersion()
    {
        var rc = vyre_wifi_get_version(out var p);
        if (rc != 0)
            throw new InvalidOperationException(GetLastError());

        return TakeUtf8StringAndFree(p);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_wifi_scan_get_results(out IntPtr out_scan_json_utf8);

    public static string ScanJson()
    {
        var rc = vyre_wifi_scan_get_results(out var p);
        if (rc != 0)
            throw new InvalidOperationException(GetLastError());

        return TakeUtf8StringAndFree(p);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int vyre_wifi_analyze_json(string scan_json_utf8, out IntPtr out_report_json_utf8);

    public static string AnalyzeJson(string scanJson)
    {
        if (string.IsNullOrWhiteSpace(scanJson))
            throw new ArgumentException("scanJson is null/empty", nameof(scanJson));

        var rc = vyre_wifi_analyze_json(scanJson, out var p);
        if (rc != 0)
            throw new InvalidOperationException(GetLastError());

        return TakeUtf8StringAndFree(p);
    }
}
