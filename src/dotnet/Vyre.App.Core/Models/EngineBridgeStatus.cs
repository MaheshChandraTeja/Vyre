namespace Vyre.App.Core.Models;

public sealed record EngineBridgeStatus(
    bool IsNativeAvailable,
    string Source,
    string LibraryName,
    string Message);
