using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Vyre.Maui.WinUI;

public partial class App : MauiWinUIApplication
{
    protected override MauiApp CreateMauiApp() => Vyre.Maui.MauiProgram.CreateMauiApp();
}
