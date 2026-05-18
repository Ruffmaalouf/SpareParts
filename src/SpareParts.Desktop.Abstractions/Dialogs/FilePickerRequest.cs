namespace SpareParts.Desktop.Abstractions.Dialogs;

public sealed class FilePickerRequest
{
    public string Title { get; init; } = "Select file";
    public string Filter { get; init; } = "All files|*.*";
    public bool AllowMultiple { get; init; }
}
