using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf
{
    public class RoleItem
    {
        public int Id                  { get; set; }
        public string Name           { get; set; } = string.Empty;
        public string Description    { get; set; } = string.Empty;
        public string BadgeColor     { get; set; } = "#22FFFFFF";
        public string BadgeTextColor { get; set; } = "#FFFFFF";
    }
}
