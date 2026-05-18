namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IApiTokenProvider
    {
        string? Token { get; set; }
        void Clear();
    }
}
