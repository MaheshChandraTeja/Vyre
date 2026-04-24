using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Vyre.App.Models;

namespace Vyre.App.Services.Engine;

internal static class NativeMethods
{
    private delegate int NativeStringCall(out IntPtr value);

    internal const string LibraryName = "vyre-interop";

    internal enum NativeStatusCode
    {
        Ok = 0,
        InvalidArgument = 1,
        NotFound = 2,
        EngineError = 3,
        NotSupported = 4,
        WifiDisabled = 5,
        NoAdapter = 6,
        PermissionDenied = 7,
        ScanFailed = 8,
        InternalError = 9
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeAccessPoint
    {
        public IntPtr Bssid;
        public IntPtr Ssid;
        public int Channel;
        public int RssiDbm;
        public int FrequencyMhz;
        public IntPtr Security;
        public int Hidden;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeScanResults
    {
        public IntPtr Items;
        public int Count;
    }

    internal sealed class ManagedScanResult
    {
        public NativeStatusCode StatusCode { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public IReadOnlyList<AccessPointViewData> Items { get; init; } = Array.Empty<AccessPointViewData>();
    }

    [DllImport(LibraryName, EntryPoint = "vyre_get_build_info", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_get_build_info(out IntPtr value);

    [DllImport(LibraryName, EntryPoint = "vyre_get_version", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_get_version(out IntPtr value);

    [SuppressMessage("Interoperability", "CA2101:Specify marshaling for P/Invoke string arguments", Justification = "The C ABI expects a UTF-8 JSON payload and the parameter is explicitly marshaled as UTF-8.")]
    [DllImport(LibraryName, EntryPoint = "vyre_analyze_json", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_analyze_json(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string scanResultsJsonUtf8,
        out IntPtr reportJsonUtf8);

    [DllImport(LibraryName, EntryPoint = "vyre_scan_start", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_scan_start(out long scanHandle);

    [DllImport(LibraryName, EntryPoint = "vyre_scan_stop", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_scan_stop(long scanHandle);

    [DllImport(LibraryName, EntryPoint = "vyre_scan_get_results", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_scan_get_results(long scanHandle, ref NativeScanResults results);

    [DllImport(LibraryName, EntryPoint = "vyre_scan_free_results", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_scan_free_results(ref NativeScanResults results);

    [DllImport(LibraryName, EntryPoint = "vyre_get_last_error", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_get_last_error(out IntPtr value);

    [DllImport(LibraryName, EntryPoint = "vyre_get_error_string", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_get_error_string(int statusCode, out IntPtr value);

    [DllImport(LibraryName, EntryPoint = "vyre_free_string", CallingConvention = CallingConvention.Cdecl)]
    private static extern void vyre_free_string(IntPtr value);

    [DllImport(LibraryName, EntryPoint = "vyre_list_capture_devices_json", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_list_capture_devices_json(out IntPtr out_json);

    [SuppressMessage("Interoperability", "CA2101:Specify marshaling for P/Invoke string arguments", Justification = "The C ABI expects UTF-8 strings for capture device, output path, and filter arguments.")]
    [DllImport(LibraryName, EntryPoint = "vyre_capture_start", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_capture_start(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string device_name_utf8,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string output_path_utf8,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string bpf_filter_utf8,
        int duration_seconds,
        out long out_capture_handle);

    [DllImport(LibraryName, EntryPoint = "vyre_capture_get_status_json", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_capture_get_status_json(long capture_handle, out IntPtr out_json);

    [DllImport(LibraryName, EntryPoint = "vyre_capture_stop", CallingConvention = CallingConvention.Cdecl)]
    private static extern int vyre_capture_stop(long capture_handle, out IntPtr out_json);

    internal static string ListCaptureDevicesJson()
    {
        var status = vyre_list_capture_devices_json(out var ptr);
        try
        {
            if (status != 0)
            {
                throw new InvalidOperationException(GetErrorString(status));
            }

            return PtrToManagedString(ptr);
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                vyre_free_string(ptr);
            }
        }
    }

    internal static long StartCapture(string deviceName, string outputPath, string bpfFilter, int durationSeconds)
    {
        var status = vyre_capture_start(deviceName, outputPath, bpfFilter ?? string.Empty, durationSeconds, out var handle);
        if (status != 0)
        {
            throw new InvalidOperationException(GetErrorString(status));
        }

        return handle;
    }

    internal static string GetCaptureStatusJson(long handle)
    {
        var status = vyre_capture_get_status_json(handle, out var ptr);
        try
        {
            if (status != 0)
            {
                throw new InvalidOperationException(GetErrorString(status));
            }

            return PtrToManagedString(ptr);
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                vyre_free_string(ptr);
            }
        }
    }

    internal static string StopCaptureJson(long handle)
    {
        var status = vyre_capture_stop(handle, out var ptr);
        try
        {
            if (status != 0)
            {
                throw new InvalidOperationException(GetErrorString(status));
            }

            return PtrToManagedString(ptr);
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                vyre_free_string(ptr);
            }
        }
    }

    private static string GetErrorString(int statusCode) =>
        ResolveError((NativeStatusCode)statusCode);

    private static string PtrToManagedString(IntPtr ptr) =>
        ReadUtf8String(ptr);

    public static string GetVersionSafe()
    {
        return TryGetString(vyre_get_version, out var value, out _)
            ? value
            : string.Empty;
    }

    public static string GetBuildInfoSafe()
    {
        return TryGetString(vyre_get_build_info, out var value, out _)
            ? value
            : string.Empty;
    }

    public static string GetLastErrorMessage()
    {
        return TryGetString(vyre_get_last_error, out var value, out _)
            ? value
            : "Unknown native error.";
    }

    public static bool TryAnalyzeJson(string scanResultsJson, out string reportJson, out string error)
    {
        try
        {
            var status = (NativeStatusCode)vyre_analyze_json(scanResultsJson, out var valuePtr);
            if (status == NativeStatusCode.Ok)
            {
                reportJson = ReadUtf8StringAndFree(valuePtr);
                error = string.Empty;
                return true;
            }

            reportJson = string.Empty;
            error = ResolveError(status, valuePtr);
            return false;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            reportJson = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    public static string SubmitScanResultsJson(string scanResultsJson)
    {
        if (TryAnalyzeJson(scanResultsJson, out var reportJson, out var error))
        {
            return reportJson;
        }

        throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
            ? "Native analysis failed."
            : error);
    }

    public static ManagedScanResult ScanOnce()
    {
        long scanHandle = 0;
        var nativeResults = new NativeScanResults();

        try
        {
            var startStatus = (NativeStatusCode)vyre_scan_start(out scanHandle);
            if (startStatus != NativeStatusCode.Ok)
            {
                return new ManagedScanResult
                {
                    StatusCode = startStatus,
                    ErrorMessage = ResolveError(startStatus)
                };
            }

            var resultsStatus = (NativeStatusCode)vyre_scan_get_results(scanHandle, ref nativeResults);
            if (resultsStatus != NativeStatusCode.Ok)
            {
                return new ManagedScanResult
                {
                    StatusCode = resultsStatus,
                    ErrorMessage = ResolveError(resultsStatus)
                };
            }

            return new ManagedScanResult
            {
                StatusCode = NativeStatusCode.Ok,
                ErrorMessage = string.Empty,
                Items = ConvertAccessPoints(nativeResults)
            };
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return new ManagedScanResult
            {
                StatusCode = NativeStatusCode.EngineError,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            if (nativeResults.Items != IntPtr.Zero || nativeResults.Count != 0)
            {
                _ = vyre_scan_free_results(ref nativeResults);
            }

            if (scanHandle != 0)
            {
                _ = vyre_scan_stop(scanHandle);
            }
        }
    }

    public static string GetReadableMessage(NativeStatusCode code, string? nativeMessage = null)
    {
        if (!string.IsNullOrWhiteSpace(nativeMessage))
        {
            return nativeMessage;
        }

        return code switch
        {
            NativeStatusCode.InvalidArgument => "The native scanner received invalid arguments.",
            NativeStatusCode.NotFound => "The requested native resource was not found.",
            NativeStatusCode.EngineError => "The native engine returned an error.",
            NativeStatusCode.NotSupported => "Native Wi-Fi scanning is not supported on this platform.",
            NativeStatusCode.WifiDisabled => "Wi-Fi appears to be disabled.",
            NativeStatusCode.NoAdapter => "No wireless adapter was found.",
            NativeStatusCode.PermissionDenied => "Permission was denied while querying the Wi-Fi adapter.",
            NativeStatusCode.ScanFailed => "The Wi-Fi scan failed.",
            NativeStatusCode.InternalError => "The native scanner failed internally.",
            _ => "Unknown native scanner error."
        };
    }

    private static bool TryGetString(NativeStringCall nativeCall, out string value, out string error)
    {
        try
        {
            var status = (NativeStatusCode)nativeCall(out var valuePtr);
            if (status == NativeStatusCode.Ok)
            {
                value = ReadUtf8StringAndFree(valuePtr);
                error = string.Empty;
                return true;
            }

            value = string.Empty;
            error = ResolveError(status, valuePtr);
            return false;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            value = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    private static List<AccessPointViewData> ConvertAccessPoints(NativeScanResults nativeResults)
    {
        var items = new List<AccessPointViewData>(Math.Max(0, nativeResults.Count));
        if (nativeResults.Items == IntPtr.Zero || nativeResults.Count <= 0)
        {
            return items;
        }

        var stride = Marshal.SizeOf<NativeAccessPoint>();
        for (var index = 0; index < nativeResults.Count; index++)
        {
            var itemPtr = IntPtr.Add(nativeResults.Items, index * stride);
            var nativeItem = Marshal.PtrToStructure<NativeAccessPoint>(itemPtr);

            items.Add(new AccessPointViewData
            {
                Bssid = ReadUtf8String(nativeItem.Bssid),
                Ssid = ReadUtf8String(nativeItem.Ssid),
                Band = FrequencyToBand(nativeItem.FrequencyMhz),
                Channel = nativeItem.Channel,
                SignalDbm = nativeItem.RssiDbm,
                Security = ReadUtf8String(nativeItem.Security)
            });
        }

        return items;
    }

    private static string ResolveError(NativeStatusCode status, IntPtr valuePtr = default)
    {
        int ErrorStringCall(out IntPtr value) => vyre_get_error_string((int)status, out value);

        var direct = ReadUtf8StringAndFree(valuePtr);
        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        if (TryGetString(ErrorStringCall, out var text, out _) &&
            !string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return GetReadableMessage(status);
    }

    private static string FrequencyToBand(int frequencyMhz)
    {
        if (frequencyMhz >= 2400 && frequencyMhz < 2500)
        {
            return "2.4 GHz";
        }

        if (frequencyMhz >= 5000 && frequencyMhz < 5925)
        {
            return "5 GHz";
        }

        if (frequencyMhz >= 5925)
        {
            return "6 GHz";
        }

        return "Unknown";
    }

    private static string ReadUtf8String(IntPtr ptr) =>
        ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr) ?? string.Empty;

    private static string ReadUtf8StringAndFree(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            return ReadUtf8String(ptr);
        }
        finally
        {
            vyre_free_string(ptr);
        }
    }
}
