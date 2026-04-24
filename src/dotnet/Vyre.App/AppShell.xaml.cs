using Vyre.App.Pages;

namespace Vyre.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("usage", typeof(UsagePage));
        Routing.RegisterRoute("doctor", typeof(DoctorPage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
    }
}
