using System;
using System.Runtime.InteropServices;

namespace Vyre.Maui.Services;

internal static class WifiNative
{
    private const string DllName = "wifi-interop";

    internal enum WifiErrorCode : int
    {
        Ok = 0,
        InvalidArgument = 1,
        ParseFailed = 2,
        Internal = 3,
        NotImplemented = 4
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr vyre_wifi_error_string(WifiErrorCode code);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr vyre_wifi_last_error_message();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void vyre_wifi_free(IntPtr p);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern WifiErrorCode vyre_wifi_get_version(out IntPtr outUtf8);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern WifiErrorCode vyre_wifi_analyze_json(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string scanJsonUtf8,
        out IntPtr outReportJsonUtf8
    );

    private static string PtrToStringUtf8(IntPtr p)
    {
        if (p == IntPtr.Zero) return string.Empty;
        return Marshal.PtrToStringUTF8(p) ?? string.Empty;
    }

    private static Exception MakeException(WifiErrorCode code)
    {
        var msgPtr = vyre_wifi_last_error_message();
        var msg = PtrToStringUtf8(msgPtr);

        var codeStr = PtrToStringUtf8(vyre_wifi_error_string(code));
        if (string.IsNullOrWhiteSpace(msg))
            msg = $"Native call failed: {codeStr} ({(int)code})";

        return new InvalidOperationException(msg);
    }

    public static string GetVersion()
    {
        var rc = vyre_wifi_get_version(out var p);
        if (rc != WifiErrorCode.Ok) throw MakeException(rc);

        try { return PtrToStringUtf8(p); }
        finally { if (p != IntPtr.Zero) vyre_wifi_free(p); }
    }

    public static string AnalyzeJson(string scanJson)
    {
        var rc = vyre_wifi_analyze_json(scanJson, out var p);
        if (rc != WifiErrorCode.Ok) throw MakeException(rc);

        try { return PtrToStringUtf8(p); }
        finally { if (p != IntPtr.Zero) vyre_wifi_free(p); }
    }
}
