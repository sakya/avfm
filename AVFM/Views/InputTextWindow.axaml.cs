using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace AVFM.Views
{
    public partial class InputTextWindow : Window
    {
        bool? m_Result = null;
        public InputTextWindow()
        {
            this.InitializeComponent();

            this.Closing += (sender, args) =>
            {
                if (m_Result == null)
                    args.Cancel = true;
            };
        }

        public InputTextWindow(string title, string label, string value)
        {
            this.InitializeComponent();
            App.SetWindowTitle(this);

            this.Title = title;
            m_Label.Content = label;

            m_TextBox.Text = value;
            m_TextBox.SelectAll();
            m_Ok.IsEnabled = !string.IsNullOrEmpty(value);
            m_TextBox.AttachedToVisualTree += (sender, args) =>
            {
                ((TextBox)sender).Focus();
            };
            m_TextBox.PropertyChanged += (sender, args) =>
            {
                if (args.Property.Name == "Text") {
                    m_Ok.IsEnabled = !string.IsNullOrEmpty(m_TextBox.Text);
                }
            };
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            m_Result = true;
            this.Close(m_TextBox.Text);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            m_Result = false;
            this.Close(null);
        }

        #region static operations
        public static async Task<string> AskInputText(Window owner, string title, string label, string value = null)
        {
            var dlg = new InputTextWindow(title, label, value);
            return await dlg.ShowDialog<string>(owner);
        } // AskInputText
        #endregion
    }
}