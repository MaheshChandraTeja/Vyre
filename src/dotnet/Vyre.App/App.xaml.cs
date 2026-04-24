using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Vyre.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_services.GetRequiredService<AppShell>());
    }
}
