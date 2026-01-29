using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Markup.Xaml;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Avalonia.Interactivity;

namespace AVFM.Controls
{
    public partial class FileOperationControl : UserControl
    {
        enum Operations
        {
            None,
            Delete,
            Copy,
            Move
        }

        private CancellationTokenSource m_CancellationTokenSource = null;
        private FileManagers.FileManagerBase m_SourceFm = null;
        private FileManagers.FileManagerBase m_DestFm = null;
        private string m_SourcePath = string.Empty;
        private string m_DestPath = string.Empty;
        private List<FileManagers.FileInfo> m_Files = null;
        private Operations m_Operation;

        #region events
        public class FileOverwriteConfirmEventArgs : EventArgs
        {
            public enum OverwriteResponse
            {
                Yes,
                No,
                YesToAll,
                NoToAll
            }

            public FileOverwriteConfirmEventArgs(FileManagers.FileInfo sourceFile, FileManagers.FileInfo destFile)
            {
                SourceFile = sourceFile;
                DestFile = destFile;
            }

            public FileManagers.FileInfo SourceFile { get; set; }
            public FileManagers.FileInfo DestFile { get; set; }
        }
        public delegate Task<FileOverwriteConfirmEventArgs.OverwriteResponse> FileOverwriteConfirmHandler(object sender, FileOverwriteConfirmEventArgs e);
        public event FileOverwriteConfirmHandler FileOverwriteConfirm;

        public event EventHandler<RoutedEventArgs> Completed;
        #endregion

        public FileOperationControl()
        {
            InitializeComponent();
        }

        public FileManagers.FileManagerBase SourceFileManager
        {
            get { return m_SourceFm; }
        }

        public FileManagers.FileManagerBase DestFileManager
        {
            get { return m_DestFm; }
        }

        public void SetFileDelete(FileManagers.FileManagerBase fileManager, List<FileManagers.FileInfo> files)
        {
            m_FilePb.IsVisible = false;
            m_Operation = Operations.Delete;
            m_SourceFm = fileManager;
            m_Files = files;
        } // SetFileDelete

        public void SetFileCopy(string sourcePath, FileManagers.FileManagerBase sourceFileManager, string destPath, FileManagers.FileManagerBase destFileManager, List<FileManagers.FileInfo> files)
        {
            m_Operation = Operations.Copy;
            m_SourceFm = sourceFileManager;
            m_SourcePath = sourcePath;
            m_DestFm = destFileManager;
            m_DestPath = destPath;
            m_Files = files;
        } // SetFileCopy

        public void SetFileMove(string sourcePath, FileManagers.FileManagerBase sourceFileManager, string destPath, FileManagers.FileManagerBase destFileManager, List<FileManagers.FileInfo> files)
        {
            m_Operation = Operations.Move;
            m_SourceFm = sourceFileManager;
            m_SourcePath = sourcePath;
            m_DestFm = destFileManager;
            m_DestPath = destPath;
            m_Files = files;
        } // SetFileMove

        public async void Start()
        {
            if (m_CancellationTokenSource != null)
                m_CancellationTokenSource.Dispose();
            m_CancellationTokenSource = new CancellationTokenSource();

            m_FilePb.Value = 0;
            m_AllPb.Maximum = m_Files.Count;
            m_AllPb.Value = 0;
            m_AllNumber.Text = "-";
            m_AllSize.Text = string.Empty;

            try {
                switch (m_Operation) {
                    case Operations.Delete:
                        await Task.Run(async () => await Delete(m_CancellationTokenSource.Token));
                        break;
                    case Operations.Copy:
                        await Task.Run(async () => await Copy(m_CancellationTokenSource.Token, false));
                        break;
                    case Operations.Move:
                        await Task.Run(async () => await Copy(m_CancellationTokenSource.Token, true));
                        break;
                }
            } catch (Exception ex) {
                await Views.MessageWindow.ShowException((Window)this.VisualRoot, null, ex);
                Completed?.Invoke(this, new RoutedEventArgs());
            }
        } // Start

        public void Abort()
        {
            if (m_CancellationTokenSource != null) {
                m_CancellationTokenSource.Cancel();
            }
        } // Abort

        private async Task<bool> Delete(CancellationToken token) {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                m_FilePb.Value = 0;
                m_FileName.Text = Localizer.Localizer.Instance["GettingFiles"];
            });

            var allFiles = await GetAllFiles(token, m_SourceFm, m_Files, false);
            var culture = ((App)App.Current).Settings.Culture;
            int idx = 0;
            foreach (var file in allFiles) {
                if (token.IsCancellationRequested)
                    break;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    m_FilePb.Value = 0;
                    m_FileName.Text = file.Name;
                    m_AllPb.Maximum = allFiles.Count;
                    m_AllNumber.Text = $"{idx.ToString("#,##0", culture)}/{allFiles.Count.ToString("#,##0", culture)}";
                });

                await m_SourceFm.Delete(file.FullPath);

                idx++;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    m_FilePb.Value = 100;
                    m_AllPb.Value = idx;
                    m_AllNumber.Text = $"{idx.ToString("#,##0", culture)}/{m_Files.Count.ToString("#,##0", culture)}";
                });
            }

            Completed?.Invoke(this, new RoutedEventArgs());
            return true;
        } // Delete

        private async Task<bool> Copy(CancellationToken token, bool deleteSource) {
            await Dispatcher.UIThread.InvokeAsync(() =>
             {
                 m_FilePb.Value = 0;
                 m_FileName.Text = Localizer.Localizer.Instance["GettingFiles"];
             });

            var allFiles = await GetAllFiles(token, m_SourceFm, m_Files, !deleteSource);
            var totalSize = allFiles.Sum(f => f.Size);
            var culture = ((App)App.Current).Settings.Culture;

            FileOverwriteConfirmEventArgs.OverwriteResponse? overwriteResponse = null;
            var idx = 0;
            foreach (var file in allFiles) {
                if (token.IsCancellationRequested)
                    break;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    m_FilePb.Value = 0;
                    m_FilePb.Maximum = file.Size;
                    m_FileName.Text = file.Name;
                    m_AllPb.Maximum = totalSize;
                    m_AllNumber.Text = $"{idx.ToString("#,##0", culture)}/{allFiles.Count.ToString("#,##0", culture)}";
                    m_AllSize.Text = $"{Utils.Utils.FormatSize((long)m_AllPb.Value)}/{Utils.Utils.FormatSize(totalSize)}";
                });

                bool skipFile = false;
                string deltaPath = string.Empty;
                if (m_SourcePath.EndsWith(m_SourceFm.GetPathSeparator()))
                    deltaPath = file.FullPath.Substring(m_SourcePath.Length);
                else
                    deltaPath = file.FullPath.Substring(m_SourcePath.Length + 1);
                var newPath = m_DestFm.CombinePath(m_DestPath, deltaPath);
                if (file.IsDirectory) {
                    if (!await m_DestFm.DirectoryExists(newPath))
                        await m_DestFm.CreateDirectory(newPath);
                } else {
                    if (await m_DestFm.FileExists(newPath)) {
                        if (overwriteResponse == FileOverwriteConfirmEventArgs.OverwriteResponse.YesToAll)
                            await m_DestFm.Delete(newPath);
                        else if (overwriteResponse == FileOverwriteConfirmEventArgs.OverwriteResponse.NoToAll)
                            skipFile = true;
                        else {
                            var destFile = await m_DestFm.GetFileInfo(newPath);
                            var res = FileOverwriteConfirm != null ? await FileOverwriteConfirm.Invoke(this, new FileOverwriteConfirmEventArgs(file, destFile)) : FileOverwriteConfirmEventArgs.OverwriteResponse.No;
                            overwriteResponse = res;
                            if (res == FileOverwriteConfirmEventArgs.OverwriteResponse.No || res == FileOverwriteConfirmEventArgs.OverwriteResponse.NoToAll)
                                skipFile = true;
                            else if (res == FileOverwriteConfirmEventArgs.OverwriteResponse.Yes || res == FileOverwriteConfirmEventArgs.OverwriteResponse.YesToAll)
                                await m_DestFm.Delete(newPath);
                        }
                    }

                    if (!skipFile) {
                        var destPath = m_DestFm.CombinePath(m_DestPath, deltaPath);
                        destPath = destPath.Remove(destPath.Length - file.Name.Length, file.Name.Length);
                        if (!await m_DestFm.DirectoryExists(destPath))
                            await m_DestFm.CreateDirectory(destPath);

                        var buffer = new byte[4096];
                        var sourceStream = await m_SourceFm.OpenFile(file.FullPath, false);
                        var destStream = await m_DestFm.OpenFile(newPath, true, file.Size);
                        int readBytes = 0;
                        while ((readBytes = sourceStream.Read(buffer, 0, buffer.Length)) > 0) {
                            if (token.IsCancellationRequested)
                                break;
                            destStream.Write(buffer, 0, readBytes);
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                m_FilePb.Value += readBytes;
                                m_AllPb.Value += readBytes;
                                m_AllSize.Text = $"{Utils.Utils.FormatSize((long)m_AllPb.Value)}/{Utils.Utils.FormatSize(totalSize)}";
                            });
                        }

                        await m_DestFm.CloseFile(destStream);
                        await m_SourceFm.CloseFile(sourceStream);
                    }
                }
                idx++;

                if (deleteSource)
                    await m_SourceFm.Delete(file.FullPath);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    m_FilePb.Value = m_FilePb.Maximum;
                    m_AllNumber.Text = $"{idx.ToString("#,##0", culture)}/{allFiles.Count.ToString("#,##0", culture)}";
                });
            }

            Completed?.Invoke(this, new RoutedEventArgs());
            return true;
        } // Copy

        private async Task<List<FileManagers.FileInfo>> GetAllFiles(CancellationToken token,
                FileManagers.FileManagerBase fileManager,
                List<FileManagers.FileInfo> files,
                bool directoryFirst)
        {
            List<FileManagers.FileInfo> res = new List<FileManagers.FileInfo>();
            foreach (var file in files) {
                if (token.IsCancellationRequested)
                    break;

                if (file.IsDirectory) {
                    if (file.IsFakeDirectory)
                        continue;

                    if (directoryFirst)
                        res.Add(file);
                    var tFiles = await fileManager.GetFileList(file.FullPath, token);
                    res.AddRange(await GetAllFiles(token, fileManager, tFiles, directoryFirst));
                    if (!directoryFirst)
                        res.Add(file);
                } else {
                    res.Add(file);
                }
            }
            return res;
        } // GetAllFiles
    }
}