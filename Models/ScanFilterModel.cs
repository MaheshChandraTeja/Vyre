namespace Vyre.Maui.Models;

public sealed record ScanFilterModel(
    string SearchText,
    bool Show24Ghz,
    bool Show5Ghz,
    bool Show6Ghz,
    bool HideHiddenSsids
);
