using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using SpareParts.Desktop.Wpf;
using SpareParts.Desktop.Wpf.Interfaces;
using Xunit;
using WpfApplication = System.Windows.Application;

namespace SpareParts.ManagementTests;

public sealed class WpfSurfaceSmokeTests
{
    public static IEnumerable<object[]> ParameterlessFeatureControls()
    {
        yield return [typeof(OwnerCockpitDashboardControl), "Owner cockpit dashboard"];
        yield return [typeof(PartCompatibilityControl), "Part compatibility"];
        yield return [typeof(DeadStockResurrectionControl), "Dead stock recovery"];
        yield return [typeof(StockArrivalTheaterControl), "Stock arrival theater"];
        yield return [typeof(PartPurchasesControl), "Part purchases"];
        yield return [typeof(UsedCarPurchasesControl), "Used car purchases"];
        yield return [typeof(UsedCarWholesaleControl), "Used car wholesale"];
        yield return [typeof(RepairPrepBoardControl), "Repair prep board"];
        yield return [typeof(ReportBuilderControl), "Report builder"];
        yield return [typeof(WhatsAppInboxControl), "WhatsApp inbox"];
        yield return [typeof(BusinessAssistantControl), "Business assistant"];
        yield return [typeof(AccountingWorkspaceControl), "Accounting workspace"];
        yield return [typeof(AccountingSetupControl), "Accounting setup"];
        yield return [typeof(ManualJournalPostingControl), "Manual journal"];
    }

    public static IEnumerable<object[]> XamlSurfaces()
    {
        yield return ["src/SpareParts.Desktop.Wpf/Windows/LoginWindow.xaml", "login window"];
        yield return ["src/SpareParts.Desktop.Wpf/Windows/MainWindow.xaml", "main window"];
        yield return ["src/SpareParts.Desktop.Wpf/Windows/ManagementWindow.xaml", "management window"];
        yield return ["src/SpareParts.Desktop.Wpf/Windows/UsedCars/UsedCarGalleryWindow.xaml", "used car gallery window"];
        yield return ["src/SpareParts.Desktop.Wpf/Windows/UsedCars/UsedCarPartsWindow.xaml", "used car parts window"];
        yield return ["src/SpareParts.Desktop.Wpf/Windows/UsedCars/UsedCarListingWindow.xaml", "used car listing window"];
        yield return ["src/SpareParts.Desktop.Wpf/Windows/Inventory/PartListingWindow.xaml", "part listing window"];

        yield return ["src/SpareParts.Desktop.Wpf/Controls/Dashboard/OwnerCockpitDashboardControl.xaml", "owner cockpit dashboard"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/Inventory/PartCompatibilityControl.xaml", "part compatibility"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/Inventory/DeadStockResurrectionControl.xaml", "dead stock recovery"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/Inventory/StockArrivalTheaterControl.xaml", "stock arrival theater"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/Purchases/PartPurchasesControl.xaml", "part purchases"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/Purchases/UsedCarPurchasesControl.xaml", "used car purchases"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/UsedCars/UsedCarWholesaleControl.xaml", "used car wholesale"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/UsedCars/RepairPrepBoardControl.xaml", "repair prep board"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/Reports/ReportBuilderControl.xaml", "report builder"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/Communications/WhatsAppInboxControl.xaml", "whatsapp inbox"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/BusinessAssistant/BusinessAssistantControl.xaml", "business assistant"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/Accounting/AccountingWorkspaceControl.xaml", "accounting workspace"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/Accounting/AccountingSetupControl.xaml", "accounting setup"];
        yield return ["src/SpareParts.Desktop.Wpf/Controls/Accounting/ManualJournalPostingControl.xaml", "manual journal"];
    }

    [Fact]
    public void Wpf_feature_controls_should_load_with_interactive_surface()
    {
        RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            foreach (var testCase in ParameterlessFeatureControls())
            {
                var controlType = Assert.IsAssignableFrom<Type>(testCase[0]);
                var label = Assert.IsType<string>(testCase[1]);
                var control = Assert.IsAssignableFrom<Control>(Activator.CreateInstance(controlType));
                control.Measure(new Size(1280, 720));
                control.Arrange(new Rect(0, 0, 1280, 720));
                control.UpdateLayout();

                var interactiveCount = Descendants(control).Count(IsInteractiveControl);
                Assert.True(interactiveCount > 0, $"{label} did not expose any interactive WPF controls.");
            }
        });
    }

    [Theory]
    [MemberData(nameof(XamlSurfaces))]
    public void Wpf_xaml_surfaces_should_keep_form_controls(string relativePath, string label)
    {
        var xaml = File.ReadAllText(RepoPath(relativePath));
        var hasInteractiveMarkup = Regex.IsMatch(
            xaml,
            @"<(Button|ToggleButton|TextBox|PasswordBox|ComboBox|DatePicker|DataGrid|ListBox|ListView|TabControl|CheckBox|RadioButton|ContentControl)\b",
            RegexOptions.CultureInvariant);

        Assert.True(hasInteractiveMarkup, $"{label} has no recognizable form/navigation controls.");
    }

    [Fact]
    public void Search_controls_should_handle_inline_run_click_sources()
    {
        RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            var controls = new UserControl[]
            {
                new PartSearchControl(new StubPartsApiClient()),
                new CustomerSearchControl(new RecordingCrudApiClient()),
                new RoleSearchControl(),
                new WarehouseSearchControl(new RecordingCrudApiClient())
            };

            foreach (var control in controls)
            {
                var method = control.GetType().GetMethod(
                    "IsInsideControl",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(method);
                var result = method!.Invoke(control, [new Run("inline result text")]);
                Assert.False(Assert.IsType<bool>(result));
            }
        });
    }

    private static bool IsInteractiveControl(DependencyObject dependencyObject)
    {
        return dependencyObject is ButtonBase
            or TextBox
            or PasswordBox
            or ComboBox
            or DatePicker
            or DataGrid
            or ListBox
            or ListView
            or TabControl
            or CheckBox
            or RadioButton;
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var queue = new Queue<DependencyObject>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;

            var count = VisualTreeHelper.GetChildrenCount(current);
            for (var index = 0; index < count; index++)
            {
                queue.Enqueue(VisualTreeHelper.GetChild(current, index));
            }
        }
    }

    private static void EnsureApplicationResources()
    {
        var app = WpfApplication.Current ?? new WpfApplication();
        if (app.Resources.Count == 0 && app.Resources.MergedDictionaries.Count == 0)
        {
            app.Resources.MergedDictionaries.Add(LoadAppResourceDictionary());
        }

        ServiceLocator.Provider = new SmokeServiceProvider();
        Assert.NotEmpty(app.Resources.MergedDictionaries);
    }

    private static ResourceDictionary LoadAppResourceDictionary()
    {
        var xaml = File.ReadAllText(RepoPath("src/SpareParts.Desktop.Wpf/App.xaml"));
        var start = xaml.IndexOf("<ResourceDictionary>", StringComparison.Ordinal);
        var end = xaml.LastIndexOf("</ResourceDictionary>", StringComparison.Ordinal);
        if (start < 0 || end < start)
        {
            throw new InvalidOperationException("Could not find App.xaml resource dictionary.");
        }

        var dictionaryXaml = xaml[start..(end + "</ResourceDictionary>".Length)]
            .Replace(
                "<ResourceDictionary>",
                "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                StringComparison.Ordinal)
            .Replace(
                "Source=\"/Themes/AuroraTheme.xaml\"",
                "Source=\"/SpareParts.Desktop.Wpf;component/Themes/AuroraTheme.xaml\"",
                StringComparison.Ordinal);

        using var stringReader = new StringReader(dictionaryXaml);
        using var xmlReader = XmlReader.Create(stringReader);
        return Assert.IsType<ResourceDictionary>(XamlReader.Load(xmlReader));
    }

    private static void RunOnStaThread(Action action)
    {
        ExceptionDispatchInfo? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                WpfApplication.Current?.Dispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        exception?.Throw();
    }

    private static string RepoPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SpareParts.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }

    private sealed class SmokeServiceProvider : IServiceProvider
    {
        private readonly ICrudApiClient _crudApi = new RecordingCrudApiClient();
        private readonly IPartsApiClient _partsApi = new StubPartsApiClient();

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ICrudApiClient))
            {
                return _crudApi;
            }

            if (serviceType == typeof(IPartsApiClient))
            {
                return _partsApi;
            }

            return null;
        }
    }
}
