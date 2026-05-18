namespace SpareParts.Desktop.Abstractions.Dialogs;

public interface IFilePickerService
{
    IReadOnlyList<string> PickFiles(FilePickerRequest request);
}
