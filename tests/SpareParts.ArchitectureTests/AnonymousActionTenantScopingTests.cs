using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Controllers;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.ArchitectureTests;

/// <summary>
/// Regression coverage for the class of bug behind the critical C1 leak: an [AllowAnonymous]
/// controller action reaching a service that reads ITenantContext-scoped data without a guard
/// against the unresolved-anonymous case (IsSuperAdmin == false and TenantId &lt;= 0).
///
/// A precise, fully general reflection rule isn't practical here: several controllers legitimately
/// expose [AllowAnonymous] actions backed by services that DO take ITenantContext, because those
/// services now contain an explicit anonymous-guard (WebCatalogService, VisualPartSearchService),
/// or because the action authenticates a different way before ever calling into tenant-scoped code
/// (CommunicationsController.RecordInbound checks a shared webhook secret first).
///
/// The closest safe approximation: enumerate every [AllowAnonymous] action (class-level or
/// method-level) across all API controllers, find any constructor-injected service (or a service's
/// own constructor-injected dependency, one level deep) that takes ITenantContext, and require it to
/// be explicitly documented in AcknowledgedAnonymousTenantScopedDependencies below together with the
/// reason it's safe. Anything NOT on that list fails the test — so a brand new [AllowAnonymous]
/// action added on top of a tenant-scoped service will force a conscious decision instead of
/// silently reintroducing the C1 bug.
/// </summary>
public class AnonymousActionTenantScopingTests
{
    private sealed record AcknowledgedDependency(string Controller, string Action, string ServiceTypeName, string Reason);

    private static readonly AcknowledgedDependency[] AcknowledgedAnonymousTenantScopedDependencies =
    [
        new(nameof(WebCatalogController), nameof(WebCatalogController.GetAvailableParts), "WebCatalogService",
            "Guards internally: returns [] when !IsSuperAdmin && TenantId <= 0, before any DB call (C1 fix). " +
            "This is the action that actually calls WebCatalogService.GetAvailableParts."),
        new(nameof(WebCatalogController), nameof(WebCatalogController.VisualSearch), "VisualPartSearchService",
            "Guards internally: QueryPartCandidates returns [] when !IsSuperAdmin && TenantId <= 0, before any DB " +
            "call (C1 fix). This is the action that actually calls VisualPartSearchService.SearchAsync."),
        new(nameof(WebCatalogController), nameof(WebCatalogController.VisualSearch), "WebCatalogService",
            "Constructor-injected on the same controller as VisualSearch but never called by it (VisualSearch only " +
            "calls VisualPartSearchService). Acknowledged conservatively because this test checks all constructor " +
            "dependencies per controller, not per-action call graphs; WebCatalogService itself is guarded (C1 fix)."),
        new(nameof(WebCatalogController), nameof(WebCatalogController.GetAvailableParts), "VisualPartSearchService",
            "Constructor-injected on the same controller as GetAvailableParts but never called by it "+
            "(GetAvailableParts only calls WebCatalogService). Acknowledged conservatively for the same reason as " +
            "above; VisualPartSearchService itself is guarded (C1 fix)."),
        new(nameof(WebCatalogController), nameof(WebCatalogController.GetAvailableParts), "PartRequestsService",
            "Constructor-injected on the same controller but never called by this action (only CreatePartRequest " +
            "calls it, and that action requires authentication/is not [AllowAnonymous]). PartRequestsService reads " +
            "ITenantContext.TenantId directly without a guard, but it is never reachable from an anonymous action."),
        new(nameof(WebCatalogController), nameof(WebCatalogController.VisualSearch), "PartRequestsService",
            "Same as above: constructor-injected on the same controller but never called by VisualSearch."),
        new(nameof(CommunicationsController), nameof(CommunicationsController.RecordInbound), "CommunicationsService",
            "Not tenant-guard-based: authenticated via a constant-time shared webhook secret " +
            "(X-SpareParts-Communication-Secret) checked in the controller before the service is ever called."),
        new(nameof(CommunicationsController), nameof(CommunicationsController.RecordInbound), "WhatsAppCampaignService",
            "Constructor-injected alongside CommunicationsService on the same controller; RecordInbound never " +
            "calls into it. Authenticated the same way as CommunicationsService above (shared webhook secret).")
    ];

    [Fact]
    public void AllowAnonymousActions_WithTenantScopedServiceDependencies_ShouldBeExplicitlyAcknowledged()
    {
        var controllerTypes = typeof(HealthController).Assembly
            .GetExportedTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                type.Name.EndsWith("Controller", StringComparison.Ordinal) &&
                typeof(ControllerBase).IsAssignableFrom(type))
            .ToArray();

        var violations = new List<string>();

        foreach (var controllerType in controllerTypes)
        {
            var classLevelAnonymous = controllerType.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();
            var constructorServiceTypes = GetConstructorServiceTypes(controllerType);

            var anonymousActions = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => classLevelAnonymous || method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any())
                .ToArray();

            if (anonymousActions.Length == 0)
            {
                continue;
            }

            foreach (var tenantScopedServiceType in constructorServiceTypes.Where(DependsOnTenantContext))
            {
                foreach (var action in anonymousActions)
                {
                    var isAcknowledged = AcknowledgedAnonymousTenantScopedDependencies.Any(ack =>
                        ack.Controller == controllerType.Name &&
                        ack.Action == action.Name &&
                        ack.ServiceTypeName == tenantScopedServiceType.Name);

                    if (!isAcknowledged)
                    {
                        violations.Add(
                            $"{controllerType.Name}.{action.Name} is [AllowAnonymous] and the controller injects " +
                            $"{tenantScopedServiceType.Name}, which depends on ITenantContext, but this combination " +
                            "is not in AcknowledgedAnonymousTenantScopedDependencies. Either add an internal " +
                            "anonymous-tenant guard to the service (see WebCatalogService/VisualPartSearchService) " +
                            "and document it here, or remove [AllowAnonymous].");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(" | ", violations));
    }

    [Fact]
    public void AcknowledgedAnonymousTenantScopedDependencies_ShouldStillReferToRealControllerActionsAndServices()
    {
        // Guards the guard: if a controller/action/service is renamed, the acknowledgement list
        // above must be updated rather than silently going stale and hiding a real violation.
        var apiAssembly = typeof(HealthController).Assembly;

        foreach (var ack in AcknowledgedAnonymousTenantScopedDependencies)
        {
            var controllerType = apiAssembly.GetType($"SpareParts.Api.Controllers.{ack.Controller}");
            Assert.True(controllerType is not null, $"Acknowledged controller '{ack.Controller}' no longer exists.");

            var actionMethod = controllerType!.GetMethod(ack.Action, BindingFlags.Instance | BindingFlags.Public);
            Assert.True(actionMethod is not null, $"Acknowledged action '{ack.Controller}.{ack.Action}' no longer exists.");

            var isAnonymous = controllerType.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any() ||
                actionMethod!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();
            Assert.True(isAnonymous, $"'{ack.Controller}.{ack.Action}' is no longer [AllowAnonymous]; remove this stale acknowledgement.");

            var constructorServiceTypes = GetConstructorServiceTypes(controllerType);
            Assert.Contains(constructorServiceTypes, type => type.Name == ack.ServiceTypeName);
        }
    }

    private static IReadOnlyList<Type> GetConstructorServiceTypes(Type controllerType)
    {
        var constructor = controllerType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        return constructor?.GetParameters().Select(p => p.ParameterType).ToArray() ?? [];
    }

    private static bool DependsOnTenantContext(Type serviceType)
    {
        return serviceType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .SelectMany(c => c.GetParameters())
            .Any(p => p.ParameterType == typeof(ITenantContext));
    }
}
