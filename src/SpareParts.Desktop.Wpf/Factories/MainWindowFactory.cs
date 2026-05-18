using Microsoft.Extensions.DependencyInjection;
using System;

namespace SpareParts.Desktop.Wpf
{
    public sealed class MainWindowFactory : IMainWindowFactory
    {
        private readonly IServiceProvider _services;

        public MainWindowFactory(IServiceProvider services)
        {
            _services = services;
        }

        public MainWindow Create()
            => _services.GetRequiredService<MainWindow>();
    }
}
