using SpareParts.Domain.Auth;
using System.ComponentModel;

namespace SpareParts.Desktop.Wpf
{
    // WPF display wrapper — adds UI-only computed properties to UserDto
    public class UserManagementDto : UserDto, INotifyPropertyChanged
    {
        public string LastLoginDisplay =>
            LastLoginAt.HasValue
                ? LastLoginAt.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm")
                : "Never";

        public string StatusBadge => IsActive ? "Active" : "Inactive";

        public static UserManagementDto FromUser(UserDto user)
        {
            return new UserManagementDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        public void Notify(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
