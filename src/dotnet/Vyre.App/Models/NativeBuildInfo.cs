namespace Vyre.App.Models;

public sealed record NativeBuildInfo(
    bool IsNativeAvailable,
    string LibraryName,
    string Message,
    string Source);
