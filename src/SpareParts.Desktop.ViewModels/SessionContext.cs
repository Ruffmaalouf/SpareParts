using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Auth;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf
{
    public static class SessionContext
    {
        public static SessionUser? CurrentUser { get; set; }
    }
}
