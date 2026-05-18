using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Reports;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class ReportBuilderApiClient : FeatureApiClientBase, IReportBuilderApiClient
    {
        public ReportBuilderApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.ApiBaseUrl)
        {
        }

        public Task<List<ReportTableDto>> GetTablesAsync()
            => RetrieveAsync<ReportTableDto>("api/reportbuilder/tables");

        public Task<List<ReportColumnDto>> GetColumnsAsync(string tableKey)
            => RetrieveAsync<ReportColumnDto>($"api/reportbuilder/columns?tableKey={Uri.EscapeDataString(tableKey ?? string.Empty)}");

        public Task<List<ReportTableLinkDto>> GetLinksAsync()
            => RetrieveAsync<ReportTableLinkDto>("api/reportbuilder/links");

        public Task<ReportJoinGraphDto> GetJoinGraphAsync()
            => RetrieveOneAsync<ReportJoinGraphDto>("api/reportbuilder/join-graph", "Join graph metadata was not returned.");

        public Task<ReportSchemaInspectorDto> GetSchemaInspectorAsync(string tableKey)
            => RetrieveOneAsync<ReportSchemaInspectorDto>($"api/reportbuilder/schema?tableKey={Uri.EscapeDataString(tableKey ?? string.Empty)}", "Schema inspector data was not returned.");

        public Task<ReportSecurityOptionsDto> GetSecurityOptionsAsync()
            => RetrieveOneAsync<ReportSecurityOptionsDto>("api/reportbuilder/security-options", "Report builder security options were not returned.");

        public Task<List<ReportSavedReportSummaryDto>> GetSavedReportsAsync()
            => RetrieveAsync<ReportSavedReportSummaryDto>("api/reportbuilder/saved-reports");

        public Task<ReportSavedReportDetailDto> GetSavedReportAsync(int id)
            => RetrieveOneAsync<ReportSavedReportDetailDto>($"api/reportbuilder/saved-reports/{id}", "Saved report detail was not returned.");

        public Task<ReportSavedReportDetailDto> SaveReportAsync(SaveReportDefinitionRequest request)
            => AddAsync<ReportSavedReportDetailDto>("api/reportbuilder/saved-reports", request, "Saved report detail was not returned.");

        public Task DeleteSavedReportAsync(int id)
            => DeleteResourceAsync($"api/reportbuilder/saved-reports/{id}");

        public Task<ReportSavedReportSummaryDto> SetFavoriteAsync(int id, bool isFavorite)
            => AddAsync<ReportSavedReportSummaryDto>($"api/reportbuilder/saved-reports/{id}/favorite?isFavorite={isFavorite.ToString().ToLowerInvariant()}", new { }, "Saved report favorite state was not returned.");

        public Task<List<BackgroundReportRunSummaryDto>> GetBackgroundRunsAsync()
            => RetrieveAsync<BackgroundReportRunSummaryDto>("api/reportbuilder/background-runs");

        public Task<BackgroundReportRunSummaryDto> QueueBackgroundRunAsync(QueueBackgroundReportRunRequest request)
            => AddAsync<BackgroundReportRunSummaryDto>("api/reportbuilder/background-runs", request, "Background report queue response was not returned.");

        public Task<ReportResultDto> GetBackgroundRunResultAsync(int id)
            => RetrieveOneAsync<ReportResultDto>($"api/reportbuilder/background-runs/{id}/result", "Background report result was not returned.");

        public Task<ReportTableLinkDto> SaveLinkAsync(SaveReportTableLinkRequest request)
            => AddAsync<ReportTableLinkDto>("api/reportbuilder/links", request, "Report builder link was not returned.");

        public Task DeleteLinkAsync(int id)
            => DeleteResourceAsync($"api/reportbuilder/links/{id}");

        public Task<ReportResultDto> RunReportAsync(RunReportRequest request)
            => AddAsync<ReportResultDto>("api/reportbuilder/run", request, "Report builder did not return any rows.");
    }
}
