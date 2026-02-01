using CommunityToolkit.Mvvm.ComponentModel;

namespace Vyre.Maui.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? statusMessage;
}
