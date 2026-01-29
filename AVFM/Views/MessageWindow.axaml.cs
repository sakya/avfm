using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Threading.Tasks;
using System;

namespace AVFM.Views
{
    public partial class MessageWindow : Window
    {
        private static MessageWindow m_OpenedMessageWindow = null;

        public enum Buttons
        {
            YesNo,
            OkCancel,
            Ok
        }

        public enum Icons
        {
            None,
            Error,
            Question,
            Info
        }

        public MessageWindow()
        {
        }

        public MessageWindow(string title, string message, Buttons buttons = Buttons.YesNo, Icons icon = Icons.None)
        {
            this.InitializeComponent();

            App.SetWindowTitle(this);

            this.Title = title;
            m_Message.Text = message;

            m_Button1.IsCancel = false;
            m_Button1.IsDefault = false;
            m_Button2.IsCancel = false;
            m_Button2.IsDefault = false;

            switch (buttons) {
                case Buttons.Ok:
                    m_Button2.IsVisible = false;
                    m_Button1.Content = Localizer.Localizer.Instance["Ok"];
                    m_Button1.IsCancel = true;
                    m_Button1.IsDefault = true;
                    break;
                case Buttons.OkCancel:
                    m_Button1.Content = Localizer.Localizer.Instance["Ok"];
                    m_Button1.IsDefault = true;
                    m_Button2.Content = Localizer.Localizer.Instance["Cancel"];
                    m_Button2.IsCancel = true;
                    break;
                case Buttons.YesNo:
                    m_Button1.Content = Localizer.Localizer.Instance["Yes"];
                    m_Button1.IsDefault = true;
                    m_Button2.Content = Localizer.Localizer.Instance["No"];
                    m_Button2.IsCancel = true;
                    break;
            }

            switch (icon) {
                case Icons.None:
                    m_Icon.IsVisible = false;
                    break;
                case Icons.Error:
                    m_Icon.Value = "fas fa-exclamation-triangle";
                    m_Icon.Foreground = new SolidColorBrush((Color)this.FindResource("DangerColor"));
                    break;
                case Icons.Info:
                    m_Icon.Value = "fas fa-info-circle";
                    m_Icon.Foreground = new SolidColorBrush((Color)this.FindResource("InfoColor"));
                    break;
                case Icons.Question:
                    m_Icon.Value = "fas fa-question-circle";
                    m_Icon.Foreground = new SolidColorBrush((Color)this.FindResource("InfoColor"));
                    break;
            }
        }

        private void OnButton1Click(object sender, RoutedEventArgs e)
        {
            this.Close(true);
        }

        private void OnButton2Click(object sender, RoutedEventArgs e)
        {
            this.Close(false);
        }

        #region static operations
        public static void CloseOpenedWindow()
        {
            if (m_OpenedMessageWindow != null) {
                m_OpenedMessageWindow.Close(false);
                m_OpenedMessageWindow = null;
            }
        } // CloseOpenedWindow

        public static async Task<bool> ShowMessage(Window owner, string title, string message, Icons icon = Icons.None)
        {
            CloseOpenedWindow();
            m_OpenedMessageWindow = new MessageWindow(title, message, Buttons.Ok, icon);
            await m_OpenedMessageWindow.ShowDialog<bool>(owner);

            return true;
        } // ShowMessage

        public static async Task<bool> ShowConfirmMessage(Window owner, string title, string message)
        {
            CloseOpenedWindow();
            m_OpenedMessageWindow = new MessageWindow(title, message, Buttons.YesNo, Icons.Question);
            return await m_OpenedMessageWindow.ShowDialog<bool>(owner);
        } // ShowConfirmMessage

        public static async Task<bool> ShowException(Window owner, string message, Exception ex)
        {
            string msg = !string.IsNullOrEmpty(message) ? $"{message}:\n{ex.Message}" : ex.Message;
            await ShowMessage(owner, Localizer.Localizer.Instance["Error"], msg, Views.MessageWindow.Icons.Error);
            return true;
        } // ShowError
        #endregion
    }
}
