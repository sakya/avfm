using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace AVFM.Views
{
    public partial class FileOverwriteConfirmWindow : Window
    {
        private bool? m_Result = null;
        private FileManagers.FileInfo m_SourceFile = null;
        private FileManagers.FileInfo m_DestFile = null;

        public FileOverwriteConfirmWindow()
        {
            this.InitializeComponent();
        }

        public FileOverwriteConfirmWindow(FileManagers.FileInfo sourceFile, FileManagers.FileInfo destFile)
        {
            m_SourceFile = sourceFile;
            m_DestFile = destFile;

            this.InitializeComponent();

            App.SetWindowTitle(this);

            this.Closing += (sender, args) =>
            {
                if (m_Result == null)
                    args.Cancel = true;
            };

            m_Message.Text = string.Format(Localizer.Localizer.Instance["OverwriteConfirmMessage"], m_DestFile.Name);
            m_Source.Text = $"{Utils.Utils.FormatDateTime(m_SourceFile.LastModified)}\r\n{Utils.Utils.FormatSize(m_SourceFile.Size)}";
            m_Dest.Text = $"{Utils.Utils.FormatDateTime(m_DestFile.LastModified)}\r\n{Utils.Utils.FormatSize(m_DestFile.Size)}";

            var mip = MimeIconProviders.MimeIconProviderFactory.GetMimeIconProvider(MimeIconProviders.MimeIconProviderBase.IconSizes.Size32x32);
            var mimeIcon = mip.GetMimeIcon(m_SourceFile.Type, m_SourceFile.FullPath).Result;
            if (!string.IsNullOrEmpty(mimeIcon)) {
                m_SourceIcon.Source = new Bitmap(mimeIcon);
                m_DestIcon.Source = new Bitmap(mimeIcon);
            }

        }

        private void OnYesClick(object sender, RoutedEventArgs e)
        {
            m_Result = true;
            if (m_ApplyToAll.IsChecked == true)
                this.Close(Controls.FileOperationControl.FileOverwriteConfirmEventArgs.OverwriteResponse.YesToAll);
            else
                this.Close(Controls.FileOperationControl.FileOverwriteConfirmEventArgs.OverwriteResponse.Yes);
        }

        private void OnNoClick(object sender, RoutedEventArgs e)
        {
            m_Result = false;
            if (m_ApplyToAll.IsChecked == true)
                this.Close(Controls.FileOperationControl.FileOverwriteConfirmEventArgs.OverwriteResponse.NoToAll);
            else
                this.Close(Controls.FileOperationControl.FileOverwriteConfirmEventArgs.OverwriteResponse.No);
        }
    }
}