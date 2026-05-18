using SpareParts.Desktop.Wpf.ViewModels;
using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SpareParts.Desktop.Wpf
{
    public partial class WhatsAppInboxControl : UserControl
    {
        private INotifyCollectionChanged? _messages;

        public WhatsAppInboxControl()
        {
            InitializeComponent();
            DataContextChanged += WhatsAppInboxControl_DataContextChanged;
            Loaded += (_, _) => AttachMessagesCollection();
            Unloaded += (_, _) => DetachMessagesCollection();
        }

        private void WhatsAppInboxControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachMessagesCollection();
            AttachMessagesCollection();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || DataContext is not WhatsAppInboxViewModel viewModel)
            {
                return;
            }

            e.Handled = true;
            viewModel.LoadConversationsCommand.Execute(null);
        }

        private void AttachMessagesCollection()
        {
            if (_messages != null || DataContext is not WhatsAppInboxViewModel viewModel)
            {
                return;
            }

            _messages = viewModel.Messages;
            _messages.CollectionChanged += Messages_CollectionChanged;
        }

        private void DetachMessagesCollection()
        {
            if (_messages == null)
            {
                return;
            }

            _messages.CollectionChanged -= Messages_CollectionChanged;
            _messages = null;
        }

        private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(
                new Action(() => MessagesScrollViewer.ScrollToEnd()),
                DispatcherPriority.Background);
        }
    }
}
