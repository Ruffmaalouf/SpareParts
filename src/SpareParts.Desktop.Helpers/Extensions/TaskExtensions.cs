using System;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Helpers
{
    public static class TaskExtensions
    {
        public static async void SafeFireAndForget(this Task task, Action<Exception>? onException = null)
        {
            try
            {
                await task.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }
        }
    }
}
