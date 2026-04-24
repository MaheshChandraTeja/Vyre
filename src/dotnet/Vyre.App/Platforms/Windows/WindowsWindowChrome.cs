#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT.Interop;
using WinUIBorder = Microsoft.UI.Xaml.Controls.Border;
using WinUIColumnDefinition = Microsoft.UI.Xaml.Controls.ColumnDefinition;
using WinUICornerRadius = Microsoft.UI.Xaml.CornerRadius;
using WinUIGradientStop = Microsoft.UI.Xaml.Media.GradientStop;
using WinUIGrid = Microsoft.UI.Xaml.Controls.Grid;
using WinUIGridLength = Microsoft.UI.Xaml.GridLength;
using WinUIGridUnitType = Microsoft.UI.Xaml.GridUnitType;
using WinUIHorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using WinUIImage = Microsoft.UI.Xaml.Controls.Image;
using WinUILinearGradientBrush = Microsoft.UI.Xaml.Media.LinearGradientBrush;
using WinUIOrientation = Microsoft.UI.Xaml.Controls.Orientation;
using WinUIRowDefinition = Microsoft.UI.Xaml.Controls.RowDefinition;
using WinUISolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WinUIStackPanel = Microsoft.UI.Xaml.Controls.StackPanel;
using WinUIStretch = Microsoft.UI.Xaml.Media.Stretch;
using WinUITextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WinUIThickness = Microsoft.UI.Xaml.Thickness;
using WinUIVerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;

namespace Vyre.App.Platforms.Windows;

internal static class WindowsWindowChrome
{
    private const string ChromeRootName = "VyreWindowsChromeRoot";
    private const double TitleBarHeight = 44;
    private const double CaptionButtonSafeArea = 150;

    public static void Apply(Microsoft.UI.Xaml.Window window)
    {
        if (window.Content is FrameworkElement { Name: ChromeRootName })
        {
            return;
        }

        var appWindow = GetAppWindow(window);
        appWindow.SetIcon("appicon.ico");
        appWindow.Title = "Vyre";

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            ConfigureNativeButtons(appWindow.TitleBar);
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }

        var originalContent = window.Content as FrameworkElement;
        if (originalContent is null)
        {
            return;
        }

        var chromeRoot = new WinUIGrid
        {
            Name = ChromeRootName,
            Background = Brush("#05070D")
        };
        chromeRoot.RowDefinitions.Add(new WinUIRowDefinition { Height = new WinUIGridLength(TitleBarHeight) });
        chromeRoot.RowDefinitions.Add(new WinUIRowDefinition { Height = new WinUIGridLength(1, WinUIGridUnitType.Star) });

        var titleBar = BuildTitleBar();

        WinUIGrid.SetRow(titleBar, 0);
        WinUIGrid.SetRow(originalContent, 1);
        chromeRoot.Children.Add(titleBar);
        chromeRoot.Children.Add(originalContent);

        window.Content = chromeRoot;
        window.ExtendsContentIntoTitleBar = true;
        window.SetTitleBar(titleBar);
    }

    private static AppWindow GetAppWindow(Microsoft.UI.Xaml.Window window)
    {
        var windowHandle = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        return AppWindow.GetFromWindowId(windowId);
    }

    private static WinUIGrid BuildTitleBar()
    {
        var titleBar = new WinUIGrid
        {
            Height = TitleBarHeight,
            Background = new WinUILinearGradientBrush
            {
                StartPoint = new global::Windows.Foundation.Point(0, 0),
                EndPoint = new global::Windows.Foundation.Point(1, 0),
                GradientStops =
                {
                    new WinUIGradientStop { Color = ColorHelper.FromArgb(255, 5, 7, 13), Offset = 0 },
                    new WinUIGradientStop { Color = ColorHelper.FromArgb(255, 14, 18, 33), Offset = 0.58 },
                    new WinUIGradientStop { Color = ColorHelper.FromArgb(255, 8, 11, 20), Offset = 1 }
                }
            },
            Padding = new WinUIThickness(14, 0, CaptionButtonSafeArea, 0)
        };

        titleBar.ColumnDefinitions.Add(new WinUIColumnDefinition { Width = WinUIGridLength.Auto });
        titleBar.ColumnDefinitions.Add(new WinUIColumnDefinition { Width = WinUIGridLength.Auto });
        titleBar.ColumnDefinitions.Add(new WinUIColumnDefinition { Width = WinUIGridLength.Auto });
        titleBar.ColumnDefinitions.Add(new WinUIColumnDefinition { Width = new WinUIGridLength(1, WinUIGridUnitType.Star) });

        var iconFrame = new WinUIBorder
        {
            Width = 28,
            Height = 28,
            CornerRadius = new WinUICornerRadius(8),
            BorderBrush = Brush("#334155"),
            BorderThickness = new WinUIThickness(1),
            Background = Brush("#111827"),
            Child = new WinUIImage
            {
                Source = new BitmapImage(new Uri("ms-appx:///appiconLogo.targetsize-32.png")),
                Width = 20,
                Height = 20,
                Stretch = WinUIStretch.Uniform
            }
        };

        var titleStack = new WinUIStackPanel
        {
            Orientation = WinUIOrientation.Horizontal,
            VerticalAlignment = WinUIVerticalAlignment.Center,
            Spacing = 8
        };
        titleStack.Children.Add(new WinUITextBlock
        {
            Text = "Vyre",
            Foreground = Brush("#F8FAFC"),
            FontSize = 14,
            FontWeight = new global::Windows.UI.Text.FontWeight { Weight = 700 },
            VerticalAlignment = WinUIVerticalAlignment.Center
        });
        titleStack.Children.Add(new WinUIBorder
        {
            Padding = new WinUIThickness(8, 2, 8, 3),
            CornerRadius = new WinUICornerRadius(999),
            BorderBrush = Brush("#24324D"),
            BorderThickness = new WinUIThickness(1),
            Background = Brush("#0B1220"),
            Child = new WinUITextBlock
            {
                Text = "Network Intelligence",
                Foreground = Brush("#94A3B8"),
                FontSize = 11,
                FontWeight = new global::Windows.UI.Text.FontWeight { Weight = 600 },
                VerticalAlignment = WinUIVerticalAlignment.Center
            }
        });

        var divider = new WinUIBorder
        {
            Width = 1,
            Height = 22,
            Background = Brush("#1E293B"),
            Margin = new WinUIThickness(18, 0, 14, 0)
        };

        var statusText = new WinUITextBlock
        {
            Text = "Desktop",
            Foreground = Brush("#64748B"),
            FontSize = 12,
            FontWeight = new global::Windows.UI.Text.FontWeight { Weight = 600 },
            VerticalAlignment = WinUIVerticalAlignment.Center
        };

        WinUIGrid.SetColumn(iconFrame, 0);
        WinUIGrid.SetColumn(titleStack, 1);
        WinUIGrid.SetColumn(divider, 2);
        WinUIGrid.SetColumn(statusText, 3);

        iconFrame.VerticalAlignment = WinUIVerticalAlignment.Center;
        titleStack.Margin = new WinUIThickness(10, 0, 0, 0);

        titleBar.Children.Add(iconFrame);
        titleBar.Children.Add(titleStack);
        titleBar.Children.Add(divider);
        titleBar.Children.Add(statusText);
        titleBar.Children.Add(new WinUIBorder
        {
            Height = 1,
            Background = Brush("#111827"),
            VerticalAlignment = WinUIVerticalAlignment.Bottom,
            HorizontalAlignment = WinUIHorizontalAlignment.Stretch
        });

        return titleBar;
    }

    private static void ConfigureNativeButtons(AppWindowTitleBar titleBar)
    {
        titleBar.ButtonBackgroundColor = ColorHelper.FromArgb(0, 0, 0, 0);
        titleBar.ButtonInactiveBackgroundColor = ColorHelper.FromArgb(0, 0, 0, 0);
        titleBar.ButtonForegroundColor = ColorHelper.FromArgb(255, 226, 232, 240);
        titleBar.ButtonInactiveForegroundColor = ColorHelper.FromArgb(255, 100, 116, 139);
        titleBar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(255, 30, 41, 59);
        titleBar.ButtonHoverForegroundColor = ColorHelper.FromArgb(255, 248, 250, 252);
        titleBar.ButtonPressedBackgroundColor = ColorHelper.FromArgb(255, 51, 65, 85);
        titleBar.ButtonPressedForegroundColor = ColorHelper.FromArgb(255, 248, 250, 252);
    }

    private static WinUISolidColorBrush Brush(string hex)
    {
        return new WinUISolidColorBrush(ParseColor(hex));
    }

    private static global::Windows.UI.Color ParseColor(string hex)
    {
        var value = hex.TrimStart('#');
        var red = Convert.ToByte(value[..2], 16);
        var green = Convert.ToByte(value.Substring(2, 2), 16);
        var blue = Convert.ToByte(value.Substring(4, 2), 16);
        return ColorHelper.FromArgb(255, red, green, blue);
    }
}
#endif
