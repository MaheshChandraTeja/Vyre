using Vyre.App.ViewModels;

namespace Vyre.App.Pages;

public partial class HomePage : ContentPage
{
    private bool _loaded;

    public HomePage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<HomePageViewModel>();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        if (BindingContext is HomePageViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }
}
