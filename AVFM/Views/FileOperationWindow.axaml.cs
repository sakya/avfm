using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace AVFM.Views
{
    public partial class FileOperationWindow : Window
    {
        bool? m_Result = null;

        public FileOperationWindow()
        {
            this.InitializeComponent();

            App.SetWindowTitle(this);

            this.Closing += (sender, args) => {
                if (m_Result == null)
                    args.Cancel = true;
            };

            m_Operation.FileOverwriteConfirm += async (sender, args) =>
            {
                Controls.FileOperationControl.FileOverwriteConfirmEventArgs.OverwriteResponse res = Controls.FileOperationControl.FileOverwriteConfirmEventArgs.OverwriteResponse.No;
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    args.SourceFile.Type = await Task.Run(() => m_Operation.SourceFileManager.GetFileMimeType(args.SourceFile.FullPath));
                    args.DestFile.Type = args.SourceFile.Type;
                    var dlg = new FileOverwriteConfirmWindow(args.SourceFile, args.DestFile);
                    res = await dlg.ShowDialog<Controls.FileOperationControl.FileOverwriteConfirmEventArgs.OverwriteResponse>((Window)this.VisualRoot);
                });
                return res;
            };

            m_Operation.Completed += (sender, args) => {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    m_Result = true;
                    this.Close(true);
                });
            };
        }

        public void SetFileDelete(FileManagers.FileManagerBase fileManager, List<FileManagers.FileInfo> files)
        {
            Title = Localizer.Localizer.Instance["DeleteTitle"];
            m_Operation.SetFileDelete(fileManager, files);
        } // SetFileDelete

        public void SetFileCopy(string sourcePath, FileManagers.FileManagerBase sourceFileManager, string destPath, FileManagers.FileManagerBase destFileManager, List<FileManagers.FileInfo> files)
        {
            Title = Localizer.Localizer.Instance["CopyTitle"];
            m_Operation.SetFileCopy(sourcePath, sourceFileManager, destPath, destFileManager, files);
        } // SetFileCopy

        public void SetFileMove(string sourcePath, FileManagers.FileManagerBase sourceFileManager, FileManagers.FileManagerBase destFileManager, string destPath, List<FileManagers.FileInfo> files)
        {
            Title = Localizer.Localizer.Instance["CopyTitle"];
            m_Operation.SetFileMove(sourcePath, sourceFileManager, destPath, destFileManager, files);
        } // SetFileMove

        public void Start()
        {
            m_Operation.Start();
        } // Start

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            m_Operation.Abort();
            if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                (desktop.MainWindow as MainWindow).ShowNotification(Avalonia.Controls.Notifications.NotificationType.Warning, Localizer.Localizer.Instance["Message"], Localizer.Localizer.Instance["OperationAbortedByUser"]);
            }
        } // OnCancelClicked
    }
}