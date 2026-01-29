using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using System;
using Avalonia.Reactive;

namespace AVFM.Controls
{
    public partial class TitleBar : UserControl
    {
        public TitleBar()
        {
            InitializeComponent();

            this.IsVisible = Environment.OSVersion.Platform == PlatformID.Win32NT;
            CanMinimize = true;
            CanMaximize = true;
        }

        public bool CanMinimize { get; set; }
        public bool CanMaximize { get; set; }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            var pw = (Window)this.VisualRoot;
            if (pw != null) {
                SetTitle(pw.Title);
                var title = pw.GetObservable(Window.TitleProperty);
                title.Subscribe(new AnonymousObserver<string>(value =>
                {
                    SetTitle(value);
                }));

                var wState = pw.GetObservable(Window.WindowStateProperty);
                wState.Subscribe(new AnonymousObserver<WindowState>(s =>
                {
                    if (s == WindowState.Maximized) {
                        pw.Padding = new Thickness(5);
                        m_MaximizeBtn.Content = new Projektanker.Icons.Avalonia.Icon() { Value = "fas fa-window-restore" };
                    } else {
                        pw.Padding = new Thickness(0);
                        m_MaximizeBtn.Content = new Projektanker.Icons.Avalonia.Icon() { Value = "fas fa-window-maximize" };
                    }
                }));
            }

            m_MinimizeBtn.Click += (e, a) =>
            {
                ((Window)this.VisualRoot).WindowState = WindowState.Minimized;
            };
            m_MinimizeBtn.IsVisible = CanMinimize;

            m_MaximizeBtn.Click += (e, a) =>
            {
                var pw = (Window)this.VisualRoot;
                if (pw.WindowState == WindowState.Maximized)
                    pw.WindowState = WindowState.Normal;
                else
                    pw.WindowState = WindowState.Maximized;
            };
            m_MaximizeBtn.IsVisible = CanMinimize;

            m_CloseBtn.Click += (e, a) =>
            {
                ((Window)this.VisualRoot).Close();
            };
        }

        private void SetTitle(string title)
        {
            m_Title.Text = title;
        }
    }
}
