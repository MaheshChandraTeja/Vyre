using Android.App;
using Android.Content.PM;
using Android.OS;
using Google.Android.Material.Navigation;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Vyre.App.Pages;
using AndroidView = Android.Views.View;
using AndroidViewGroup = Android.Views.ViewGroup;

namespace Vyre.App;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int AttachRetryDelayMilliseconds = 250;
    private const int MaxAttachAttempts = 12;
    private const string MoreTabContentDescription = "More";

    private Shell? _shell;
    private NavigationBarView? _navigationBarView;
    private AndroidView? _moreTabView;
    private int _attachAttempts;
    private bool _isResettingMoreNavigation;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ScheduleAttachMoreTabReselectionHandler();
    }

    protected override void OnResume()
    {
        base.OnResume();
        ScheduleAttachMoreTabReselectionHandler();
    }

    protected override void OnDestroy()
    {
        DetachMoreTabClickHandler();

        if (_navigationBarView is not null)
        {
            _navigationBarView.ItemReselected -= OnBottomNavigationItemReselected;
            _navigationBarView = null;
        }

        if (_shell is not null)
        {
            _shell.Navigated -= OnShellNavigated;
            _shell = null;
        }

        base.OnDestroy();
    }

    private void ScheduleAttachMoreTabReselectionHandler()
    {
        _attachAttempts = 0;
        Window?.DecorView.Post(AttachMoreTabReselectionHandler);
    }

    private void AttachMoreTabReselectionHandler()
    {
        if (_navigationBarView?.Parent is not null)
        {
            AttachShellNavigationHandler();
            UpdateMoreTabClickHandler();
            return;
        }

        AttachShellNavigationHandler();

        var navigationBarView = FindDescendant<NavigationBarView>(Window?.DecorView);
        if (navigationBarView is null)
        {
            _attachAttempts++;

            if (_attachAttempts < MaxAttachAttempts)
            {
                Window?.DecorView.PostDelayed(
                    AttachMoreTabReselectionHandler,
                    AttachRetryDelayMilliseconds);
            }

            return;
        }

        if (_navigationBarView is not null)
        {
            _navigationBarView.ItemReselected -= OnBottomNavigationItemReselected;
        }

        navigationBarView.ItemReselected += OnBottomNavigationItemReselected;
        _navigationBarView = navigationBarView;
        UpdateMoreTabClickHandler();
    }

    private void AttachShellNavigationHandler()
    {
        var shell = Shell.Current;
        if (ReferenceEquals(_shell, shell))
        {
            return;
        }

        if (_shell is not null)
        {
            _shell.Navigated -= OnShellNavigated;
        }

        shell.Navigated += OnShellNavigated;
        _shell = shell;
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        Window?.DecorView.Post(UpdateMoreTabClickHandler);
    }

    private void UpdateMoreTabClickHandler()
    {
        var moreTabView = FindViewByContentDescription(
            Window?.DecorView,
            MoreTabContentDescription);

        if (moreTabView is null || !moreTabView.Selected)
        {
            DetachMoreTabClickHandler();
            return;
        }

        if (!ReferenceEquals(_moreTabView, moreTabView))
        {
            DetachMoreTabClickHandler();
            _moreTabView = moreTabView;
            _moreTabView.Click += OnMoreTabViewClicked;
        }

        _moreTabView.Clickable = true;
    }

    private void DetachMoreTabClickHandler()
    {
        if (_moreTabView is null)
        {
            return;
        }

        _moreTabView.Click -= OnMoreTabViewClicked;
        _moreTabView = null;
    }

    private async void OnMoreTabViewClicked(object? sender, EventArgs e)
    {
        if (_isResettingMoreNavigation ||
            sender is not AndroidView view ||
            !view.Selected)
        {
            return;
        }

        _isResettingMoreNavigation = true;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(ResetMoreStackAsync);
        }
        finally
        {
            _isResettingMoreNavigation = false;
        }
    }

    private async void OnBottomNavigationItemReselected(object? sender, NavigationBarView.ItemReselectedEventArgs e)
    {
        var moreTabView = FindViewByContentDescription(
            Window?.DecorView,
            MoreTabContentDescription);

        if (_isResettingMoreNavigation ||
            moreTabView is null ||
            !moreTabView.Selected)
        {
            return;
        }

        _isResettingMoreNavigation = true;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(ResetMoreStackAsync);
        }
        finally
        {
            _isResettingMoreNavigation = false;
        }
    }

    private static async Task ResetMoreStackAsync()
    {
        var currentPage = Shell.Current.CurrentPage;
        if (currentPage is MorePage)
        {
            return;
        }

        var navigation = currentPage?.Navigation;
        var navigationStack = navigation?.NavigationStack;
        if (navigation is not null &&
            navigationStack is { Count: > 1 } &&
            navigationStack[0] is MorePage)
        {
            await navigation.PopToRootAsync(false);
            return;
        }

        await Shell.Current.GoToAsync("//more");
    }

    private static T? FindDescendant<T>(AndroidView? view)
        where T : AndroidView
    {
        if (view is T typedView)
        {
            return typedView;
        }

        if (view is not AndroidViewGroup viewGroup)
        {
            return null;
        }

        for (var i = 0; i < viewGroup.ChildCount; i++)
        {
            var match = FindDescendant<T>(viewGroup.GetChildAt(i));
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static AndroidView? FindViewByContentDescription(
        AndroidView? view,
        string contentDescription)
    {
        if (view is null)
        {
            return null;
        }

        if (string.Equals(
            view.ContentDescription,
            contentDescription,
            StringComparison.OrdinalIgnoreCase))
        {
            return view;
        }

        if (view is not AndroidViewGroup viewGroup)
        {
            return null;
        }

        for (var i = 0; i < viewGroup.ChildCount; i++)
        {
            var match = FindViewByContentDescription(
                viewGroup.GetChildAt(i),
                contentDescription);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
