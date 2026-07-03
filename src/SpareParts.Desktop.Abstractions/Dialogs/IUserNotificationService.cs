namespace SpareParts.Desktop.Abstractions.Dialogs;

public interface IUserNotificationService
{
    void Show(string message, string title, NotificationKind kind = NotificationKind.Info);

    /// <summary>
    /// Shows a Yes/No confirmation dialog and returns true if the user confirmed.
    /// </summary>
    bool Confirm(string message, string title);
}
