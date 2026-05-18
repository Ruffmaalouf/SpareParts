using SpareParts.Desktop.Wpf.Interfaces;

namespace SpareParts.Desktop.Wpf
{
    public sealed class ApiTokenProvider : IApiTokenProvider
    {
        private string? _token;

        public string? Token
        {
            get => _token;
            set => _token = value;
        }

        public void Clear() => _token = null;
    }
}
