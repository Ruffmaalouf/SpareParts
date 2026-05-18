using SpareParts.Domain.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IReportBuilderApiClient
    {
        Task<List<ReportTableDto>> GetTablesAsync();
        Task<List<ReportColumnDto>> GetColumnsAsync(string tableKey);
        Task<List<ReportTableLinkDto>> GetLinksAsync();
        Task<ReportJoinGraphDto> GetJoinGraphAsync();
        Task<ReportSchemaInspectorDto> GetSchemaInspectorAsync(string tableKey);
        Task<ReportSecurityOptionsDto> GetSecurityOptionsAsync();
        Task<List<ReportSavedReportSummaryDto>> GetSavedReportsAsync();
        Task<ReportSavedReportDetailDto> GetSavedReportAsync(int id);
        Task<ReportSavedReportDetailDto> SaveReportAsync(SaveReportDefinitionRequest request);
        Task DeleteSavedReportAsync(int id);
        Task<ReportSavedReportSummaryDto> SetFavoriteAsync(int id, bool isFavorite);
        Task<List<BackgroundReportRunSummaryDto>> GetBackgroundRunsAsync();
        Task<BackgroundReportRunSummaryDto> QueueBackgroundRunAsync(QueueBackgroundReportRunRequest request);
        Task<ReportResultDto> GetBackgroundRunResultAsync(int id);
        Task<ReportTableLinkDto> SaveLinkAsync(SaveReportTableLinkRequest request);
        Task DeleteLinkAsync(int id);
        Task<ReportResultDto> RunReportAsync(RunReportRequest request);
    }
}
