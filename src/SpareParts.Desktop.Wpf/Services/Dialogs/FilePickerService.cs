using Microsoft.Win32;
using SpareParts.Desktop.Abstractions.Dialogs;

namespace SpareParts.Desktop.Wpf.Services.Dialogs;

public sealed class FilePickerService : IFilePickerService
{
    public IReadOnlyList<string> PickFiles(FilePickerRequest request)
    {
        var dialog = new OpenFileDialog
        {
            Title = request.Title,
            Filter = request.Filter,
            Multiselect = request.AllowMultiple
        };

        return dialog.ShowDialog() == true
            ? dialog.FileNames
            : Array.Empty<string>();
    }
}
