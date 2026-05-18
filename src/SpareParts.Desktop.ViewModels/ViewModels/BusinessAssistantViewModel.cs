using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.BusinessAssistant;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class BusinessAssistantViewModel : INotifyPropertyChanged
    {
        private readonly IBusinessAssistantApiClient _assistantApi;
        private string _question = string.Empty;
        private string _status = "Ready.";
        private Brush _statusBrush = Brushes.LightGreen;
        private bool _isLoading;

        public BusinessAssistantViewModel(IBusinessAssistantApiClient assistantApi)
        {
            _assistantApi = assistantApi;
            AskCommand = new RelayCommand(_ => AskAsync().SafeFireAndForget(HandleBackgroundException));
            RunActionCommand = new RelayCommand(RunAction);
            UseSuggestionCommand = new RelayCommand(UseSuggestion);

            StarterActions.Add(new BusinessAssistantActionDto
            {
                Id = "create_report",
                Label = "Create report",
                Description = "Pick a quick operating report and run it from live data.",
                Kind = "Report",
                Tone = "Neutral"
            });
            StarterActions.Add(new BusinessAssistantActionDto
            {
                Id = "send_payment_reminder",
                Label = "Send reminder",
                Description = "Prepare payment reminder text from unpaid invoices.",
                Kind = "Draft",
                Target = "whatsapp",
                Tone = "Good"
            });
            StarterActions.Add(new BusinessAssistantActionDto
            {
                Id = "open_customer",
                Label = "Open customer",
                Description = "Open the top customer summary from live balances.",
                Kind = "Open",
                Target = "contacts",
                Tone = "Neutral"
            });
            StarterActions.Add(new BusinessAssistantActionDto
            {
                Id = "draft_purchase_order",
                Label = "Draft purchase order",
                Description = "Build a low-stock purchase shortlist.",
                Kind = "Draft",
                Target = "purchase-parts",
                Tone = "Good"
            });
            StarterActions.Add(new BusinessAssistantActionDto
            {
                Id = "find_unpaid_customers",
                Label = "Find unpaid customers",
                Description = "List customer balances from unpaid sales invoices.",
                Kind = "Query",
                Tone = "Warning"
            });
            StarterActions.Add(new BusinessAssistantActionDto
            {
                Id = "campaign_dead_stock_clearance",
                Label = "Dead stock clearance",
                Description = "Suggest a campaign for dormant on-hand parts.",
                Kind = "Campaign",
                Target = "whatsapp",
                Tone = "Warning"
            });
            StarterActions.Add(new BusinessAssistantActionDto
            {
                Id = "campaign_back_in_stock",
                Label = "Back in stock campaign",
                Description = "Feature recently received available parts.",
                Kind = "Campaign",
                Target = "whatsapp",
                Tone = "Good"
            });
            StarterActions.Add(new BusinessAssistantActionDto
            {
                Id = "campaign_unpaid_reminders",
                Label = "Unpaid reminders",
                Description = "Prepare a receivables reminder campaign.",
                Kind = "Campaign",
                Target = "whatsapp",
                Tone = "Warning"
            });
            StarterActions.Add(new BusinessAssistantActionDto
            {
                Id = "campaign_seasonal_service_parts",
                Label = "Seasonal service parts",
                Description = "Promote maintenance parts customers may need now.",
                Kind = "Campaign",
                Target = "whatsapp",
                Tone = "Good"
            });

            Suggestions.Add("Create a report");
            Suggestions.Add("Draft purchase order");
            Suggestions.Add("Open customer");
            Suggestions.Add("Send unpaid reminder");
            Suggestions.Add("Find unpaid customers");
            Suggestions.Add("Dead stock clearance campaign");
            Suggestions.Add("Parts back in stock campaign");
            Suggestions.Add("Unpaid reminders campaign");
            Suggestions.Add("Seasonal service parts campaign");
            Suggestions.Add("Show slow-moving BMW parts over $100 bought before March");
            Suggestions.Add("How much do I owe supplier <name>?");

            Messages.Add(new BusinessAssistantMessageViewModel(false, "Ready for business actions."));
        }

        public ObservableCollection<BusinessAssistantMessageViewModel> Messages { get; } = new();
        public ObservableCollection<BusinessAssistantActionDto> StarterActions { get; } = new();
        public ObservableCollection<string> Suggestions { get; } = new();

        public ICommand AskCommand { get; }
        public ICommand RunActionCommand { get; }
        public ICommand UseSuggestionCommand { get; }

        public string Question
        {
            get => _question;
            set
            {
                if (_question == value)
                {
                    return;
                }

                _question = value;
                OnPropertyChanged(nameof(Question));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value)
                {
                    return;
                }

                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string Status
        {
            get => _status;
            private set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set
            {
                if (_statusBrush == value)
                {
                    return;
                }

                _statusBrush = value;
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public async Task AskAsync()
        {
            var prompt = Question.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Status = "Enter a question.";
                StatusBrush = Brushes.LightGoldenrodYellow;
                return;
            }

            Messages.Add(new BusinessAssistantMessageViewModel(true, prompt));
            Question = string.Empty;
            IsLoading = true;
            Status = "Checking business data...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var response = await _assistantApi.AskAsync(new BusinessAssistantQueryRequest
                {
                    Question = prompt
                });

                Messages.Add(new BusinessAssistantMessageViewModel(false, response.Answer, response.Insights, response.Actions));
                ReplaceSuggestions(response.Suggestions);

                Status = response.IsSupported ? "Ready." : "Question not recognized.";
                StatusBrush = response.IsSupported ? Brushes.LightGreen : Brushes.LightGoldenrodYellow;
            }
            catch (Exception ex)
            {
                Messages.Add(new BusinessAssistantMessageViewModel(false, $"I could not reach the business assistant API. {ex.Message}"));
                Status = "Assistant unavailable.";
                StatusBrush = Brushes.IndianRed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RunAction(object? value)
        {
            if (value is not BusinessAssistantActionDto action || string.IsNullOrWhiteSpace(action.Id))
            {
                return;
            }

            RunActionAsync(action).SafeFireAndForget(HandleBackgroundException);
        }

        private async Task RunActionAsync(BusinessAssistantActionDto action)
        {
            Messages.Add(new BusinessAssistantMessageViewModel(true, action.Label));
            IsLoading = true;
            Status = $"Running {action.Label.ToLowerInvariant()}...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var response = await _assistantApi.RunActionAsync(new BusinessAssistantActionRequest
                {
                    ActionId = action.Id,
                    Payload = action.Payload
                });

                Messages.Add(new BusinessAssistantMessageViewModel(false, response.Answer, response.Insights, response.Actions));
                ReplaceSuggestions(response.Suggestions);
                Status = response.IsSupported ? "Action complete." : "Action not recognized.";
                StatusBrush = response.IsSupported ? Brushes.LightGreen : Brushes.LightGoldenrodYellow;
            }
            catch (Exception ex)
            {
                Messages.Add(new BusinessAssistantMessageViewModel(false, $"I could not run that assistant action. {ex.Message}"));
                Status = "Assistant action unavailable.";
                StatusBrush = Brushes.IndianRed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UseSuggestion(object? value)
        {
            if (value is not string suggestion || string.IsNullOrWhiteSpace(suggestion))
            {
                return;
            }

            Question = suggestion;
            if (suggestion.Contains("<name>", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AskAsync().SafeFireAndForget(HandleBackgroundException);
        }

        private void ReplaceSuggestions(IEnumerable<string> suggestions)
        {
            Suggestions.Clear();

            foreach (var suggestion in suggestions.Where(item => !string.IsNullOrWhiteSpace(item)).Take(6))
            {
                Suggestions.Add(suggestion);
            }
        }

        private void HandleBackgroundException(Exception ex)
        {
            IsLoading = false;
            Status = ex.Message;
            StatusBrush = Brushes.IndianRed;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
