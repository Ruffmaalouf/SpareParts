using SpareParts.Domain.BusinessAssistant;
using System.Collections.ObjectModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class BusinessAssistantMessageViewModel
    {
        public BusinessAssistantMessageViewModel(
            bool isUser,
            string text,
            IEnumerable<BusinessAssistantInsightDto>? insights = null,
            IEnumerable<BusinessAssistantActionDto>? actions = null)
        {
            IsUser = isUser;
            Text = text;

            foreach (var insight in insights ?? [])
            {
                Insights.Add(insight);
            }

            foreach (var action in actions ?? [])
            {
                Actions.Add(action);
            }
        }

        public bool IsUser { get; }
        public string Text { get; }
        public ObservableCollection<BusinessAssistantInsightDto> Insights { get; } = new();
        public ObservableCollection<BusinessAssistantActionDto> Actions { get; } = new();
        public bool HasInsights => Insights.Count > 0;
        public bool HasActions => !IsUser && Actions.Count > 0;
    }
}
