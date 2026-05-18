using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SpareParts.Api.Hosting;
using SpareParts.Api.Notifications;
using SpareParts.Domain.Health;
using SpareParts.Infrastructure.Interfaces;
using SpareParts.Infrastructure.Services;
using System.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly ServiceProfile? _serviceProfile;
        private readonly ISqlConnectionFactory? _sqlConnectionFactory;
        private readonly CommunicationOptions? _communicationOptions;
        private readonly IConfiguration? _configuration;
        private readonly IServiceProvider? _serviceProvider;

        public HealthController(
            ServiceProfile? serviceProfile = null,
            ISqlConnectionFactory? sqlConnectionFactory = null,
            CommunicationOptions? communicationOptions = null,
            IConfiguration? configuration = null,
            IServiceProvider? serviceProvider = null)
        {
            _serviceProfile = serviceProfile;
            _sqlConnectionFactory = sqlConnectionFactory;
            _communicationOptions = communicationOptions;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        [HttpGet("api/health")]
        public ActionResult<HealthDashboardDto> Get()
        {
            var database = CheckDatabase();

            var dashboard = new HealthDashboardDto
            {
                Status = database.CanConnect || _sqlConnectionFactory is null ? "ok" : "degraded",
                Utc = DateTime.UtcNow,
                Service = _serviceProfile?.ServiceName ?? "SpareParts.Api",
                Capabilities = (_serviceProfile?.Capabilities ?? [])
                    .Select(capability => capability.ToString())
                    .OrderBy(capability => capability, StringComparer.Ordinal)
                    .ToArray(),
                SplitApis = BuildSplitApiStatus(),
                Database = database,
                Migrations = new MigrationsHealthDto
                {
                    Status = SparePartsApiComposition.MigrationNames.Count > 0 ? "configured" : "missing",
                    Count = SparePartsApiComposition.MigrationNames.Count,
                    Names = SparePartsApiComposition.MigrationNames
                },
                ClientConfig = BuildClientConfigStatus(),
                Notifications = BuildNotificationStatus()
            };

            return Ok(dashboard);
        }

        private DatabaseHealthDto CheckDatabase()
        {
            if (_sqlConnectionFactory is null)
            {
                return new DatabaseHealthDto
                {
                    Status = "not-configured",
                    Provider = "unknown"
                };
            }

            try
            {
                using var connection = _sqlConnectionFactory.CreateConnection();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.CommandType = CommandType.Text;
                command.ExecuteScalar();

                return new DatabaseHealthDto
                {
                    Status = "ok",
                    CanConnect = true,
                    Provider = connection.GetType().Name
                };
            }
            catch (Exception ex)
            {
                return new DatabaseHealthDto
                {
                    Status = "degraded",
                    CanConnect = false,
                    Provider = "unknown",
                    Error = ex.Message
                };
            }
        }

        private static SplitApiHealthDto[] BuildSplitApiStatus()
        {
            return SparePartsApiComposition.ExpectedServiceProfiles
                .Select(profile => new SplitApiHealthDto
                {
                    Service = profile.ServiceName,
                    Capabilities = profile.Capabilities
                        .Select(capability => capability.ToString())
                        .OrderBy(capability => capability, StringComparer.Ordinal)
                        .ToArray(),
                    HealthEndpoint = "/api/health",
                    Status = "expected"
                })
                .ToArray();
        }

        private ClientConfigHealthDto BuildClientConfigStatus()
        {
            var allowedOrigins = _configuration?
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            return new ClientConfigHealthDto
            {
                Status = "configured",
                CorsAllowedOrigins = allowedOrigins
            };
        }

        private NotificationHealthDto BuildNotificationStatus()
        {
            var signalRRegistered = _serviceProvider?
                .GetService<IHubContext<NotificationsHub>>() is not null;

            return new NotificationHealthDto
            {
                Status = signalRRegistered ? "ok" : "configured",
                HubPath = SparePartsApiComposition.NotificationsHubPath,
                SignalRRegistered = signalRRegistered,
                DeliveryProvider = _communicationOptions?.Provider ?? "Disabled",
                WebhookConfigured = _communicationOptions?.HasWebhook ?? false,
                TimeoutSeconds = _communicationOptions?.TimeoutSeconds ?? 0
            };
        }
    }
}
