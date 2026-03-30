using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace SpareParts.Desktop.Wpf
{
    public partial class CurrencyCodeSelectorControl : UserControl
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(CurrencyCodeSelectorControl),
                new PropertyMetadata("CURRENCY CODE"));

        public static readonly DependencyProperty CurrencyCodesProperty =
            DependencyProperty.Register(
                nameof(CurrencyCodes),
                typeof(IEnumerable),
                typeof(CurrencyCodeSelectorControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedCurrencyCodeProperty =
            DependencyProperty.Register(
                nameof(SelectedCurrencyCode),
                typeof(string),
                typeof(CurrencyCodeSelectorControl),
                new FrameworkPropertyMetadata(
                    "USD",
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public IEnumerable? CurrencyCodes
        {
            get => (IEnumerable?)GetValue(CurrencyCodesProperty);
            set => SetValue(CurrencyCodesProperty, value);
        }

        public string SelectedCurrencyCode
        {
            get => (string)GetValue(SelectedCurrencyCodeProperty);
            set => SetValue(SelectedCurrencyCodeProperty, value);
        }

        public CurrencyCodeSelectorControl()
        {
            InitializeComponent();
        }
    }
}
