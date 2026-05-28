using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpareParts.Api.Controllers;
using SpareParts.Api.Hosting;
using SpareParts.Api.Infrastructure;
using SpareParts.Api.Notifications;
using SpareParts.Domain.Health;
using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

public class QualityGuardrailTests
{
    private static readonly string[] CanonicalScreens =
    [
        "accounting",
        "ar",
        "business-assistant",
        "compatibility",
        "contacts",
        "dashboard",
        "dead-stock",
        "inventory",
        "invoices",
        "management",
        "manual-journal",
        "part-requests",
        "purchase-parts",
        "repair-prep",
        "report-builder",
        "settings",
        "stock",
        "stock-arrival",
        "used-car-wholesale",
        "used-car-purchases",
        "used-cars",
        "whatsapp"
    ];

    private static readonly Dictionary<string, string> DesktopScreenMap = new(StringComparer.Ordinal)
    {
        ["Accounting"] = "accounting",
        ["ManualJournal"] = "manual-journal",
        ["PartSelection"] = "inventory",
        ["Pos"] = "invoices",
        ["PartPurchases"] = "purchase-parts",
        ["Purchases"] = "used-car-purchases",
        ["UsedCarWholesale"] = "used-car-wholesale",
        ["StockArrivalTheater"] = "stock-arrival",
        ["RepairPrepBoard"] = "repair-prep",
        ["StockManagement"] = "stock",
        ["DeadStockResurrection"] = "dead-stock",
        ["PartCompatibility"] = "compatibility",
        ["BarcodeMode"] = "ar",
        ["ReportBuilder"] = "report-builder",
        ["WhatsAppInbox"] = "whatsapp",
        ["BusinessAssistant"] = "business-assistant",
        ["ArExperience"] = "ar",
        ["HomePage"] = "dashboard"
    };

    [Fact]
    public void Feature_lists_should_not_drift_between_web_mobile_and_desktop()
    {
        var expected = CanonicalScreens.OrderBy(key => key, StringComparer.Ordinal).ToArray();
        var webScreens = ParseScreenRegistry("src/SpareParts.Web.React/wwwroot/js/views/screen-registry.js").Keys
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        var mobileScreens = ParseScreenRegistry("src/SpareParts.Mobile.ReactNative/src/screens/screen-registry.js").Keys
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        var desktopScreens = ReadDesktopFeatureKeys()
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, webScreens);
        Assert.Equal(expected, mobileScreens);
        Assert.Equal(expected, desktopScreens);

        var webModules = ParseFeatureModules("src/SpareParts.Web.React/wwwroot/js/core/config.js");
        var mobileModules = ParseFeatureModules("src/SpareParts.Mobile.ReactNative/src/core/app-config.js");

        foreach (var mobileModule in mobileModules.Values)
        {
            Assert.True(webModules.TryGetValue(mobileModule.Key, out var webModule), $"Web featureModules is missing {mobileModule.Key}.");
            Assert.Equal(NormalizeEndpoint(webModule!.Endpoint), NormalizeEndpoint(mobileModule.Endpoint));
            Assert.False(string.IsNullOrWhiteSpace(mobileModule.Label), $"{mobileModule.Key} is missing a mobile label.");
            Assert.False(string.IsNullOrWhiteSpace(mobileModule.Title), $"{mobileModule.Key} is missing a mobile title.");
            Assert.NotEmpty(mobileModule.Capabilities);
        }
    }

    [Fact]
    public void Api_contract_smoke_tests_should_cover_every_screen_endpoint()
    {
        var endpoints = ReadEndpointPaths(
                "src/SpareParts.Web.React/wwwroot/js/core/config.js",
                "src/SpareParts.Mobile.ReactNative/src/core/app-config.js")
            .OrderBy(endpoint => endpoint, StringComparer.Ordinal)
            .ToArray();
        var routeTemplates = ReadControllerRouteTemplates();

        var missing = endpoints
            .Where(endpoint => routeTemplates.All(route => !RouteMatchesEndpoint(route, endpoint)))
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"Missing API route contract(s): {string.Join(", ", missing)}. Known routes: {string.Join(", ", routeTemplates.OrderBy(route => route, StringComparer.Ordinal))}");
    }

    [Fact]
    public void Lightweight_visual_navigation_smoke_tests_should_cover_web_and_mobile_navigation()
    {
        var webScreens = ParseScreenRegistry("src/SpareParts.Web.React/wwwroot/js/views/screen-registry.js");
        var webGroups = ParseNavigationGroups(
            "src/SpareParts.Web.React/wwwroot/js/components/layout.js",
            @"\{\s*key:\s*""[^""]+"",\s*label:\s*""(?<title>[^""]+)"",\s*items:\s*\[(?<items>[^\]]*)\]\s*\}");

        var mobileScreens = ParseScreenRegistry("src/SpareParts.Mobile.ReactNative/src/screens/screen-registry.js");
        var mobileGroups = ParseNavigationGroups(
            "src/SpareParts.Mobile.ReactNative/src/core/app-config.js",
            @"\{\s*title:\s*""(?<title>[^""]+)"",\s*keys:\s*\[(?<items>[^\]]*)\]\s*\}");

        AssertNavigationSmoke("web", webScreens, webGroups);
        AssertNavigationSmoke("mobile", mobileScreens, mobileGroups);
    }

    [Fact]
    public void Health_dashboard_should_cover_split_apis_database_migrations_clients_and_notifications()
    {
        using var database = new InMemorySqliteConnectionFactory();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSignalR();

        using var provider = services.BuildServiceProvider();
        var controller = new HealthController(
            new ServiceProfile("SpareParts.Inventory.Api", [ServiceCapability.Inventory, ServiceCapability.Health]),
            database,
            new CommunicationOptions
            {
                Provider = "Webhook",
                WebhookUrl = "https://example.test/spareparts/communications",
                TimeoutSeconds = 11
            },
            configuration,
            provider);

        var result = controller.Get();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dashboard = Assert.IsType<HealthDashboardDto>(ok.Value);

        Assert.Equal("ok", dashboard.Status);
        Assert.Equal("SpareParts.Inventory.Api", dashboard.Service);
        Assert.Contains("Inventory", dashboard.Capabilities);
        Assert.Contains(dashboard.SplitApis, api =>
            api.Service == "SpareParts.Inventory.Api" &&
            api.Capabilities.Contains("Inventory") &&
            api.HealthEndpoint == "/api/health");
        Assert.True(dashboard.Database.CanConnect);
        Assert.Equal("ok", dashboard.Database.Status);
        Assert.Contains(nameof(PartRequestsMigration), dashboard.Migrations.Names);
        Assert.Equal(SparePartsApiComposition.MigrationNames.Count, dashboard.Migrations.Count);
        Assert.Equal("http://localhost:5000", dashboard.ClientConfig.WebDefaultApiBaseUrl);
        Assert.Contains("http://localhost:3000", dashboard.ClientConfig.CorsAllowedOrigins);
        Assert.Equal(SparePartsApiComposition.NotificationsHubPath, dashboard.Notifications.HubPath);
        Assert.True(dashboard.Notifications.SignalRRegistered);
        Assert.True(dashboard.Notifications.WebhookConfigured);
    }

    private static IReadOnlyDictionary<string, FeatureModuleContract> ParseFeatureModules(string relativePath)
    {
        var text = ReadRepoFile(relativePath);
        var array = ExtractArray(text, "featureModules");
        var modules = new Dictionary<string, FeatureModuleContract>(StringComparer.Ordinal);

        foreach (Match match in Regex.Matches(array, @"\{(?<body>.*?)\}", RegexOptions.Singleline))
        {
            var body = match.Groups["body"].Value;
            var key = MatchString(body, "key");
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            modules[CanonicalizeKey(key)] = new FeatureModuleContract(
                CanonicalizeKey(key),
                MatchString(body, "label"),
                MatchString(body, "title"),
                MatchString(body, "endpoint"),
                MatchStringArray(body, "capabilities"));
        }

        return modules;
    }

    private static IReadOnlyDictionary<string, string> ParseScreenRegistry(string relativePath)
    {
        var text = ReadRepoFile(relativePath);
        return Regex.Matches(text, @"\{\s*key:\s*""(?<key>[^""]+)"",\s*label:\s*""(?<label>[^""]+)""")
            .Cast<Match>()
            .Select(match => new
            {
                Key = CanonicalizeKey(match.Groups["key"].Value),
                Label = match.Groups["label"].Value.Trim()
            })
            .GroupBy(screen => screen.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Label, StringComparer.Ordinal);
    }

    private static IReadOnlyList<NavigationGroupContract> ParseNavigationGroups(string relativePath, string pattern)
    {
        var text = ReadRepoFile(relativePath);
        return Regex.Matches(text, pattern, RegexOptions.Singleline)
            .Cast<Match>()
            .Select(match => new NavigationGroupContract(
                match.Groups["title"].Value.Trim(),
                ParseQuotedStrings(match.Groups["items"].Value).Select(CanonicalizeKey).ToArray()))
            .Where(group => group.Items.Count > 0)
            .ToArray();
    }

    private static void AssertNavigationSmoke(
        string appName,
        IReadOnlyDictionary<string, string> screens,
        IReadOnlyList<NavigationGroupContract> groups)
    {
        Assert.NotEmpty(groups);
        Assert.DoesNotContain(screens, screen => string.IsNullOrWhiteSpace(screen.Value));
        Assert.DoesNotContain(groups, group => string.IsNullOrWhiteSpace(group.Title));
        Assert.DoesNotContain(groups, group => group.Items.Count == 0);

        var groupedItems = groups.SelectMany(group => group.Items).ToArray();
        var duplicateItems = groupedItems
            .GroupBy(item => item, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        var missingScreens = groupedItems
            .Where(item => !screens.ContainsKey(item))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var orphanScreens = screens.Keys
            .Where(screen => !groupedItems.Contains(screen, StringComparer.Ordinal))
            .ToArray();

        Assert.True(duplicateItems.Length == 0, $"{appName} navigation has duplicated item(s): {string.Join(", ", duplicateItems)}");
        Assert.True(missingScreens.Length == 0, $"{appName} navigation points at missing screen(s): {string.Join(", ", missingScreens)}");
        Assert.True(orphanScreens.Length == 0, $"{appName} screen(s) are not reachable from navigation: {string.Join(", ", orphanScreens)}");
    }

    private static IReadOnlySet<string> ReadDesktopFeatureKeys()
    {
        var appScreenSource = ReadRepoFile("src/SpareParts.Desktop.ViewModels/Navigation/AppScreen.cs");
        var enumBody = Regex.Match(appScreenSource, @"enum\s+AppScreen\s*\{(?<body>[^}]*)\}", RegexOptions.Singleline).Groups["body"].Value;
        var keys = Regex.Matches(enumBody, @"\b(?<name>[A-Za-z][A-Za-z0-9_]*)\b")
            .Cast<Match>()
            .Select(match => match.Groups["name"].Value)
            .Where(name => DesktopScreenMap.ContainsKey(name))
            .Select(name => DesktopScreenMap[name])
            .ToHashSet(StringComparer.Ordinal);

        AddDesktopMarker(keys, "contacts",
            "src/SpareParts.Desktop.ViewModels/Management/CustomerManagementViewModel.cs",
            "src/SpareParts.Desktop.ViewModels/Management/SupplierManagementViewModel.cs");
        AddDesktopMarker(keys, "management", "src/SpareParts.Desktop.Wpf/Windows/ManagementWindow.xaml");
        AddDesktopMarker(keys, "part-requests", "src/SpareParts.Desktop.ViewModels/Management/PartRequestsManagementViewModel.cs");
        AddDesktopMarker(keys, "settings", "src/SpareParts.Desktop.Wpf/Themes/DefaultTheme.xaml");
        AddDesktopMarker(keys, "used-cars", "src/SpareParts.Desktop.ViewModels/Management/UsedCarsManagementViewModel.cs");

        return keys;
    }

    private static void AddDesktopMarker(HashSet<string> keys, string key, params string[] requiredRelativePaths)
    {
        if (requiredRelativePaths.All(relativePath => File.Exists(RepoPath(relativePath))))
        {
            keys.Add(key);
        }
    }

    private static IReadOnlyCollection<string> ReadEndpointPaths(params string[] relativePaths)
    {
        return relativePaths
            .SelectMany(relativePath => Regex.Matches(ReadRepoFile(relativePath), @"endpoint:\s*""(?<endpoint>[^""]*)""")
                .Cast<Match>()
                .Select(match => match.Groups["endpoint"].Value))
            .SelectMany(SplitEndpoint)
            .Where(endpoint => endpoint.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            .Select(NormalizeEndpoint)
            .Where(endpoint => endpoint.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyCollection<string> ReadControllerRouteTemplates()
    {
        return typeof(SalesController).Assembly
            .GetExportedTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                type.Name.EndsWith("Controller", StringComparison.Ordinal) &&
                typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(ReadControllerRouteTemplates)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> ReadControllerRouteTemplates(Type controllerType)
    {
        var controllerName = controllerType.Name[..^"Controller".Length];
        var controllerRoutes = controllerType
            .GetCustomAttributes()
            .OfType<IRouteTemplateProvider>()
            .Select(attribute => attribute.Template)
            .Where(template => template is not null)
            .DefaultIfEmpty(string.Empty)
            .Cast<string>();

        foreach (var method in controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            var actionRoutes = method
                .GetCustomAttributes()
                .OfType<IRouteTemplateProvider>()
                .ToArray();

            if (actionRoutes.Length == 0)
            {
                continue;
            }

            foreach (var controllerRoute in controllerRoutes)
            foreach (var actionRoute in actionRoutes)
            {
                yield return NormalizeRouteTemplate(CombineRouteTemplates(controllerRoute, actionRoute.Template), controllerName);
            }
        }
    }

    private static bool RouteMatchesEndpoint(string routeTemplate, string endpoint)
    {
        var pattern = Regex.Escape(routeTemplate);
        pattern = Regex.Replace(pattern, @"\\\{[^}]+\\\}", "[^/]+");
        return Regex.IsMatch(endpoint, $"^{pattern}$", RegexOptions.IgnoreCase);
    }

    private static IEnumerable<string> SplitEndpoint(string endpoint)
    {
        foreach (var part in endpoint.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var cleaned = part;
            if (Uri.TryCreate(cleaned, UriKind.Absolute, out var uri))
            {
                cleaned = uri.AbsolutePath;
            }

            yield return cleaned.Split('?', '#')[0].Trim();
        }
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        var trimmed = endpoint.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            trimmed = uri.AbsolutePath;
        }

        return trimmed.Split('?', '#')[0].Trim().Trim('/').ToLowerInvariant();
    }

    private static string NormalizeRouteTemplate(string template, string controllerName)
    {
        return template
            .Replace("[controller]", controllerName, StringComparison.OrdinalIgnoreCase)
            .Trim()
            .Trim('/')
            .ToLowerInvariant();
    }

    private static string CombineRouteTemplates(string? controllerRoute, string? actionRoute)
    {
        if (string.IsNullOrWhiteSpace(controllerRoute))
        {
            return actionRoute ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(actionRoute))
        {
            return controllerRoute;
        }

        return $"{controllerRoute.TrimEnd('/')}/{actionRoute.TrimStart('/')}";
    }

    private static string ExtractArray(string text, string variableName)
    {
        var marker = variableName;
        var markerIndex = text.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException($"Could not find {variableName} array.");
        }

        var start = text.IndexOf('[', markerIndex);
        if (start < 0)
        {
            throw new InvalidOperationException($"Could not find {variableName} array.");
        }

        var depth = 0;
        for (var index = start; index < text.Length; index++)
        {
            if (text[index] == '[')
            {
                depth++;
            }
            else if (text[index] == ']')
            {
                depth--;
                if (depth == 0)
                {
                    return text[start..(index + 1)];
                }
            }
        }

        throw new InvalidOperationException($"Could not parse {variableName} array.");
    }

    private static string MatchString(string text, string property)
    {
        return Regex.Match(text, $@"\b{Regex.Escape(property)}:\s*""(?<value>[^""]*)""").Groups["value"].Value.Trim();
    }

    private static IReadOnlyList<string> MatchStringArray(string text, string property)
    {
        var match = Regex.Match(text, $@"\b{Regex.Escape(property)}:\s*\[(?<value>[^\]]*)\]", RegexOptions.Singleline);
        return match.Success ? ParseQuotedStrings(match.Groups["value"].Value) : [];
    }

    private static IReadOnlyList<string> ParseQuotedStrings(string text)
    {
        return Regex.Matches(text, @"""(?<value>[^""]*)""")
            .Cast<Match>()
            .Select(match => match.Groups["value"].Value.Trim())
            .Where(value => value.Length > 0)
            .ToArray();
    }

    private static string CanonicalizeKey(string key)
    {
        return key.Trim().Equals("parts", StringComparison.Ordinal) ? "inventory" : key.Trim();
    }

    private static string ReadRepoFile(string relativePath)
    {
        return File.ReadAllText(RepoPath(relativePath));
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

    private sealed record FeatureModuleContract(
        string Key,
        string Label,
        string Title,
        string Endpoint,
        IReadOnlyList<string> Capabilities);

    private sealed record NavigationGroupContract(string Title, IReadOnlyList<string> Items);
}
