using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Interactivity;
namespace AVFM.Controls
{
    public partial class CheckableMenuItem : MenuItem, IStyleable
    {
        public CheckableMenuItem()
        {
            InitializeComponent();

            if (string.IsNullOrEmpty(Group)) {
                Icon = new Projektanker.Icons.Avalonia.Icon()
                {
                    Value = "far fa-square"
                };
            } else {
                Icon = new Projektanker.Icons.Avalonia.Icon();
            }
        } // CheckableMenuItem

        public new static readonly DirectProperty<CheckableMenuItem, bool> IsCheckedProperty =
            AvaloniaProperty.RegisterDirect<CheckableMenuItem, bool>(
                nameof(IsChecked),
                o => o.IsChecked,
                (o, v) => o.IsChecked = v);

        Type IStyleable.StyleKey => typeof(MenuItem);
        private bool m_IsChecked;
        private string m_Group = string.Empty;

        public event EventHandler<RoutedEventArgs> IsCheckedChanged;

        public string Group {
            get { return m_Group; }
            set {
                if (m_Group != value) {
                    m_Group = value;
                    SetIcon();
                }
            }
        }

        public new bool IsChecked
        {
            get { return m_IsChecked; }
            set {
                if (SetAndRaise(IsCheckedProperty, ref m_IsChecked, value)) {
                    SetIcon();
                    OnIsCheckedChanged();
                }
            }
        }

        private bool HasGroup
        {
            get { return !string.IsNullOrEmpty(Group); }
        }

        private void OnClicked(object sender, RoutedEventArgs args)
        {
            if (!HasGroup) {
                IsChecked = !IsChecked;
            } else if (!IsChecked)
                IsChecked = true;
        } // OnClicked

        private void OnIsCheckedChanged()
        {
            if (HasGroup) {
                if (!IsChecked)
                    return;

                if (Parent is MenuItem pi) {
                    foreach (var i in pi.Items) {
                        if (i is CheckableMenuItem mi && mi.Group == Group) {
                            mi.IsChecked = mi == this;
                        }
                    }
                }
                IsCheckedChanged?.Invoke(this, new RoutedEventArgs());
            } else {
                IsCheckedChanged?.Invoke(this, new RoutedEventArgs());
            }
        } // OnIsCheckedChanged

        private void SetIcon()
        {
            string icon;
            if (m_IsChecked)
                icon = HasGroup ? "far fa-dot-circle" : "fas fa-check-square";
            else
                icon = HasGroup ? "far fa-circle" : "far fa-square";

            if (Icon == null || (Icon as Projektanker.Icons.Avalonia.Icon)?.Value != icon) {
                Icon = new Projektanker.Icons.Avalonia.Icon()
                {
                    Value = icon
                };
            }
        } // SetIcon
    }
}