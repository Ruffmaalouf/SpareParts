using System;
using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>
    /// Customer-360 read model returned by GET /api/clients/{id}/workspace (Ignition redesign,
    /// Report 04 §05): header + balance/aging + KPIs only — child collections (invoices, payments,
    /// quotes, part requests, timeline) are separate paged calls per Report 04 §06.
    /// </summary>
    public sealed class IgnitionClientWorkspaceDto
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }

        /// <summary>"Retail" | "Wholesale" | "Fleet" ...</summary>
        public string CustomerType { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public IgnitionClientBalanceDto Balance { get; set; } = new();
        public IgnitionClientKpisDto Kpis { get; set; } = new();
        public List<IgnitionClientVehicleDto> Vehicles { get; set; } = new();
        public DateTime? LastActivityAt { get; set; }
        public IgnitionCacheMetaDto CacheMeta { get; set; } = new();
    }
}
