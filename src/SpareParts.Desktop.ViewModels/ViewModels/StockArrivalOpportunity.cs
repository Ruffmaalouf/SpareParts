using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class StockArrivalOpportunity
    {
        private StockArrivalOpportunity(
            string laneKey,
            string type,
            string title,
            string subtitle,
            string metric,
            string actionLabel,
            AppScreen? targetScreen,
            string targetLabel,
            Brush accentBrush,
            IEnumerable<StockArrivalEvidenceRow> evidence)
        {
            LaneKey = laneKey;
            Type = type;
            Title = title;
            Subtitle = subtitle;
            Metric = metric;
            ActionLabel = actionLabel;
            TargetScreen = targetScreen;
            TargetLabel = targetLabel;
            AccentBrush = accentBrush;
            Evidence = new ObservableCollection<StockArrivalEvidenceRow>(evidence);
            SearchText = $"{type} {title} {subtitle} {metric}".ToLowerInvariant();
        }

        public string LaneKey { get; }
        public string Type { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string Metric { get; }
        public string ActionLabel { get; }
        public AppScreen? TargetScreen { get; }
        public string TargetLabel { get; }
        public Brush AccentBrush { get; }
        public ObservableCollection<StockArrivalEvidenceRow> Evidence { get; }
        public string SearchText { get; }

        public static StockArrivalOpportunity Create(
            string laneKey,
            string type,
            string title,
            string subtitle,
            string metric,
            string actionLabel,
            AppScreen? targetScreen,
            string targetLabel,
            Brush accentBrush,
            params (string Label, string Value)[] evidence)
            => new(
                laneKey,
                type,
                title,
                subtitle,
                metric,
                actionLabel,
                targetScreen,
                targetLabel,
                accentBrush,
                evidence.Select(item => new StockArrivalEvidenceRow(item.Label, item.Value)));
    }
}
