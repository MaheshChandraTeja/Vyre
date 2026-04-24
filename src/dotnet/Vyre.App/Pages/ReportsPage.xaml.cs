using System.Reflection;
using System.Text.Json;
using System.Windows.Input;
using Vyre.App.ViewModels;
using System.Text.Encodings.Web;

namespace Vyre.App.Pages;

public partial class ReportsPage : ContentPage
{

    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ReportsViewModel _viewModel;

    public ReportsPage(ReportsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnOpenJsonClicked(object sender, EventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is null)
        {
            return;
        }

        var report = bindable.BindingContext;

        JsonTitle.Text = BuildJsonTitle(report);

        await ExecuteOpenReportCommandAsync(report);

        var json = ReadStringProperty(BindingContext, "SelectedReportJson");

        if (string.IsNullOrWhiteSpace(json))
        {
            json = BuildFallbackJson(report);
        }

        JsonText.Text = PrettyJson(json);
        JsonModal.IsVisible = true;
    }

    private void OnCloseJsonClicked(object sender, EventArgs e)
    {
        JsonModal.IsVisible = false;
    }

    private void OnJsonBackdropTapped(object sender, TappedEventArgs e)
    {
        JsonModal.IsVisible = false;
    }

    private async Task ExecuteOpenReportCommandAsync(object report)
    {
        if (BindingContext is not ReportsViewModel viewModel)
        {
            return;
        }

        if (report is Vyre.App.Models.ReportSummary summary &&
            viewModel.OpenReportCommand.CanExecute(summary))
        {
            await viewModel.OpenReportCommand.ExecuteAsync(summary);
        }
    }

    private static string BuildJsonTitle(object report)
    {
        var title = ReadStringProperty(report, "Title");

        if (string.IsNullOrWhiteSpace(title))
        {
            return "report.json";
        }

        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            title = title.Replace(invalid, '-');
        }

        return $"{title}.json";
    }


    private static string ReadStringProperty(object? source, string propertyName)
    {
        if (source is null)
            return string.Empty;

        var property = source
            .GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

        return property?.GetValue(source)?.ToString() ?? string.Empty;
    }

    private static string BuildFallbackJson(object report)
    {
        var payload = new
        {
            schema = "vyre.report.preview",
            title = ReadStringProperty(report, "Title"),
            createdUtc = ReadStringProperty(report, "CreatedUtc"),
            summary = new
            {
                networks = ReadStringProperty(report, "NetworkCount"),
                issues = ReadStringProperty(report, "IssueCount")
            }
        };

        return JsonSerializer.Serialize(payload, IndentedJsonOptions);
    }

    private static string PrettyJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "{\n  \"message\": \"No report payload available.\"\n}";

        try
        {
            using var document = JsonDocument.Parse(raw);

            return JsonSerializer.Serialize(document.RootElement, IndentedJsonOptions);
        }
        catch
        {
            return raw;
        }
    }
}