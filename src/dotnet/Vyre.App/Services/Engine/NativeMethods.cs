using System.Runtime.InteropServices;
using System.Text;

namespace Vyre.App.Services.Engine;

internal static class NativeMethods
{
#if IOS
    internal const string LibraryName = "__Internal";
#else
    internal const string LibraryName = "vyre-interop";
#endif

    [DllImport(LibraryName, EntryPoint = "vyre_get_build_info", CallingConvention = CallingConvention.Cdecl)]
    private static extern int VyreGetBuildInfo(byte[] buffer, int bufferLength);

    internal static bool TryGetBuildInfo(out string value, out string error)
    {
        try
        {
            var buffer = new byte[512];
            var required = VyreGetBuildInfo(buffer, buffer.Length);

            if (required > buffer.Length)
            {
                buffer = new byte[required];
                required = VyreGetBuildInfo(buffer, buffer.Length);
            }

            value = DecodeUtf8(buffer);
            error = string.Empty;
            return !string.IsNullOrWhiteSpace(value) && required > 0;
        }
        catch (DllNotFoundException ex)
        {
            value = string.Empty;
            error = ex.Message;
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            value = string.Empty;
            error = ex.Message;
            return false;
        }
        catch (BadImageFormatException ex)
        {
            value = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    private static string DecodeUtf8(byte[] buffer)
    {
        var terminator = Array.IndexOf(buffer, (byte)0);
        if (terminator < 0)
        {
            terminator = buffer.Length;
        }

        return Encoding.UTF8.GetString(buffer, 0, terminator).Trim();
    }
}
