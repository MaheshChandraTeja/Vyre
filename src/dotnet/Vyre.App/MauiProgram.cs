using Microsoft.Extensions.Logging;
using Vyre.App.Services.Engine;
using Vyre.App.ViewModels;

namespace Vyre.App;

public static class MauiProgram
{
    public static IServiceProvider Services { get; private set; } = default!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(_ => { });

        builder.Services.AddSingleton<IVyreEngineService, VyreEngineService>();
        builder.Services.AddSingleton<HomePageViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        Services = app.Services;
        return app;
    }
}
