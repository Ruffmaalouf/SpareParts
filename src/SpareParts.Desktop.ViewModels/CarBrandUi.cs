using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.ViewModels;
using SpareParts.Domain.Sales;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

// CarBrand / CarModel local UI-only models kept here because they are purely
// presentational (hold LogoPath, AvailableCars, etc.) and are NOT persisted.
// All API-crossing DTOs come from SpareParts.Domain.

namespace SpareParts.Desktop.Wpf
{
    // ── UI-only display models (never sent to API) ────────────────────────────
    public class CarBrandUi
    {
        public int    Id      { get; set; }
        public string Name    { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string LogoPath { get; set; } = string.Empty;
    }
}
