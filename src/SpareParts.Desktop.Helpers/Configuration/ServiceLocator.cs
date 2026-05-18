using System;

namespace SpareParts.Desktop.Wpf
{
    public static class ServiceLocator
    {
        public static IServiceProvider Provider { get; set; } = null!;

        public static T Resolve<T>() where T : class
        {
            var service = Provider.GetService(typeof(T)) as T;
            return service ?? throw new InvalidOperationException($"Service '{typeof(T).Name}' is not registered.");
        }
    }
}
