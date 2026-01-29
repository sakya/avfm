using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Input;
using System.Linq;
using Avalonia.Media;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia.VisualTree;
using System.Text;

namespace AVFM.Controls
{
    public partial class FileManagerControl : UserControl
    {
        class History {
            public History()
            {
                Positions = new List<string>();
            }
            public List<string> Positions { get; set; }
            public int? CurrentIndex { get; set;}

            public bool CanGoBack {
                get {
                    return CurrentIndex > 0;
                }
            }

            public bool CanGoForward {
                get {
                    return CurrentIndex + 1 < Positions.Count;
                }
            }

            public string GoBack()
            {
                if (CanGoBack) {
                    CurrentIndex--;
                    return Positions[CurrentIndex.Value];
                }
                return null;
            }

            public string GoForward()
            {
                if (CanGoForward) {
                    CurrentIndex++;
                    return Positions[CurrentIndex.Value];
                }
                return null;
            }
        }

        public class PositionChangedEventArgs : EventArgs
        {
            public PositionChangedEventArgs(string oldPosition, string newPosition)
            {
                OldPosition = oldPosition;
                NewPosition = newPosition;
            }

            public string OldPosition { get; set; }
            public string NewPosition { get; set; }
        }
        public delegate void PositionChangedHandler(object sender, PositionChangedEventArgs e);
        public event PositionChangedHandler PositionChanged;
        public event FileListingControl.FileTriggeredHandler FileCopyRequest;
        public event FileListingControl.FileTriggeredHandler FileMoveRequest;
        public event EventHandler<RoutedEventArgs> IsActiveChanged;

        private bool m_IsActive = false;
        private FileManagers.FileManagerBase m_FileManager = null;
        private CancellationTokenSource m_RefreshCancellationToken = null;
        private SemaphoreSlim m_RefreshSema = new SemaphoreSlim(1,1);
        private FileManagers.DriveInfo m_DriveInfo = null;
        private History m_History = new History();
        private DispatcherTimer m_FilterTimer = null;
        private List<FileManagers.FileInfo> m_UnfilteredFiles = null;
        private FileListingControl.ViewModes m_ViewMode = FileListingControl.ViewModes.List;
        private bool m_ShowHiddenFiles = false;

        public FileManagerControl()
        {
            InitializeComponent();

            m_FileListing.FileTriggered += async (sender, args) =>
            {
                if (args.Files.Count == 1 && args.Files[0].IsDirectory) {
                    await SetPosition(args.Files[0].FullPath, true, args.Files[0].Name == ".." ? Position : null, true);
                } else {
                    foreach (var file in args.Files) {
                        try {
                            m_FileManager.OpenFileWithDefaultApplication(file.FullPath);
                        }
                        catch (Exception ex) {
                            await ShowError(null, ex);
                        }
                    }
                }
            };

            m_FileListing.FileDeleteRequest += DeleteFiles;
            m_FileListing.FileCopyRequest += (sender, args) => {
                FileCopyRequest?.Invoke(this, args);
            };
            m_FileListing.FileMoveRequest += (sender, args) => {
                FileMoveRequest?.Invoke(this, args);
            };
            m_FileListing.SelectionChanged += OnSelectionChanged;

            m_PositionTextBox.PropertyChanged += (sender, args) =>
            {
                if (args.Property.Name == "Text")
                    OnPositionChanged(m_PositionTextBox);
            };

            m_FilterTextbox.PropertyChanged += (sender, args) =>
            {
                if (args.Property.Name == "Text")
                    OnFilterChanged(m_FilterTextbox);
            };
        }

        public bool IsActive
        {
            get { return m_IsActive; }
            set {
                if (m_IsActive != value) {
                    m_IsActive = value;
                    OnIsActiveChanged();
                }
            }
        }

        public List<FileManagers.FileInfo> Files {
            get {
                return m_FileListing.Files;
            }
        }

        public FileListingControl.ViewModes ViewMode
        {
            get { return m_ViewMode; }
            set {
                if (value != m_ViewMode) {
                    m_FileListing.ViewMode = value;
                    m_ViewMode = value;
                }
            }
        }

        public bool ShowHiddenFiles
        {
            get { return m_ShowHiddenFiles; }
            set {
                if (value != m_ShowHiddenFiles) {
                    m_ShowHiddenFiles = value;
                    DispatcherTimer.RunOnce(async () => await Refresh(m_FileListing.CurrentRow?.FullPath), TimeSpan.FromMilliseconds(100));
                }
            }
        }

        public string Position {
            get;
            private set;
        }

        public string PositionName {
            get;
            private set;
        }

        public FileManagers.FileManagerBase FileManager
        {
            get { return m_FileManager; }
        }

        private Utils.Settings AppSettings
        {
            get { return ((App)App.Current).Settings; }
        }

        public SolidColorBrush InactiveColor { get; set; } = new(Color.FromArgb(40, 0, 0, 0));

        public async Task<bool> SetPosition(string newPosition, bool isNew = true, string selectedFile = null, bool keepFileManager = true) {
            if (Position != newPosition) {
                var oldPosition = Position;
                if (m_FileManager == null || !keepFileManager) {
                    if (m_FileManager != null)
                        await Task.Run( () => m_FileManager.Dispose());
                    m_FileManager = FileManagers.FileManagerFactory.GetFileManager(newPosition, App.DefaultFsFileManager, out newPosition);
                }
                m_FileListing.FileManager = m_FileManager;
                var di = await m_FileManager.GetPositionInfo(newPosition);
                PositionName = di.Name;
                Position = di.FullName;
                m_PositionTextBox.Text = Position;

                if (isNew) {
                    m_History.Positions.Add(Position);
                    m_History.CurrentIndex = m_History.Positions.Count - 1;
                }

                m_GoBack.IsEnabled = m_History.CanGoBack;
                m_GoForward.IsEnabled = m_History.CanGoForward;
                var rt = Refresh(selectedFile);

                PositionChanged?.Invoke(this, new PositionChangedEventArgs(oldPosition, Position));
                await rt;
            }
            return true;
        } // SetPosition

        public async void RefreshPosition()
        {
            await Refresh(m_FileListing.CurrentRow?.FullPath);
        } // RefreshPosition

        public async void ManageKeyDown(KeyEventArgs args)
        {
            // Shortcuts
            var sh = AppSettings.GetShortcut(args);
            if (sh != null) {
                switch (sh.Type) {
                    case Utils.Settings.Shortcut.Shortcuts.GoBack:
                        OnGoBackClicked(null, null);
                        break;
                    case Utils.Settings.Shortcut.Shortcuts.GoForward:
                        OnGoForwardClicked(null, null);
                        break;
                    case Utils.Settings.Shortcut.Shortcuts.GoUp:
                        OnUpClicked(null, null);
                        break;
                    case Utils.Settings.Shortcut.Shortcuts.GoHome:
                        OnHomeClicked(null, null);
                        break;
                    case Utils.Settings.Shortcut.Shortcuts.Refresh:
                        args.Handled = true;
                        await Refresh(m_FileListing.CurrentRow?.FullPath);
                        break;
                    case Utils.Settings.Shortcut.Shortcuts.MakeDir:
                        args.Handled = true;
                        CreateDirectory();
                        break;
                    case Utils.Settings.Shortcut.Shortcuts.RenameFile:
                        if (m_FileListing.CurrentRow != null) {
                            args.Handled = true;
                            RenameFile(m_FileListing.CurrentRow);
                        }
                        break;
                }
            }

            // Address
            if (m_PositionTextBox.IsKeyboardFocusWithin) {
                args.Handled = true;
                if (args.Key == Key.Return && !string.IsNullOrEmpty(m_PositionTextBox.Text)) {
                    await SetPosition(m_PositionTextBox.Text, true);
                    m_FileListing.Focus();
                }
            }

            // Filtering
            if (!args.Handled && IsFilterKey(m_FilterTextbox.IsFocused, args)) {
                if (!m_FilterTextbox.IsFocused) {
                    m_FilterTextbox.IsVisible = true;
                    m_FilterTextbox.Focus();
                } else if (args.Key == Key.Escape) {
                    m_FilterTextbox.Text = string.Empty;
                    args.Handled = true;
                }
            }

            // File listing
            if (!args.Handled)
                m_FileListing.ManageKeyDown(args);
        } // ManageKeyDown

        #region Events
        private void OnIsActiveChanged()
        {
            //m_FileListing.Background = IsActive ? ActiveColor : new SolidColorBrush(Colors.Transparent);
            m_FileListing.Background = IsActive ? new SolidColorBrush(Colors.Transparent) : InactiveColor;
            if (IsActive) {
                foreach (var fm in ((Window)VisualRoot).GetVisualDescendants().OfType<Controls.FileManagerControl>()) {
                    if (fm != this)
                        fm.IsActive = false;
                }
            }
            IsActiveChanged?.Invoke(this, new RoutedEventArgs());
        } // OnIsActiveChanged

        private void OnGotFocus(object sender, GotFocusEventArgs args)
        {
            if (m_FilterTextbox.IsVisible)
                m_FilterTextbox.Focus();
        }

        private async void OnUpClicked(object sender, RoutedEventArgs args)
        {
            var up = Files.Where(f => f.Name == "..").FirstOrDefault();
            if (up != null) {
                await SetPosition(up.FullPath, true, Position);
            }
        } // OnUpClicked

        private async void OnHomeClicked(object sender, RoutedEventArgs args)
        {
            await SetPosition(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), true, null, false);
        } // OnHomeClicked

        private async void OnGoBackClicked(object sender, RoutedEventArgs args)
        {
            var newPos = m_History.GoBack();
            if (!string.IsNullOrEmpty(newPos)) {
                await SetPosition(newPos, false);
            }
        } // OnGoBackClicked

        private async void OnGoForwardClicked(object sender, RoutedEventArgs args)
        {
            var newPos = m_History.GoForward();
            if (!string.IsNullOrEmpty(newPos)) {
                await SetPosition(newPos, false);
            }
        } // OnGoForwardClicked

        private async void OnRefreshClicked(object sender, RoutedEventArgs args)
        {
            await Refresh(m_FileListing.CurrentRow?.FullPath);
        } // OnRefreshClicked

        private async void OnRootClicked(object sender, RoutedEventArgs args)
        {
            await SetPosition(m_FileManager.GetRoot());
        } // OnRootClicked

        private void OnBookmarksClicked(object sender, RoutedEventArgs args)
        {
            m_BookmarksList.ItemsSource = null;
            m_BookmarksList.ItemsSource = AppSettings.Bookmarks.OrderBy(b => b.Name);
            m_BookmarksPopup.IsOpen = !m_BookmarksPopup.IsOpen;
        } // OnBookmarksClicked

        private async void OnSelectedBookmarkChanged(object sender, SelectionChangedEventArgs args)
        {
            m_BookmarksPopup.IsOpen = false;

            if (args.AddedItems.Count == 1) {
                var bm = args.AddedItems[0] as FileManagers.FileManagerBase.Bookmark;
                await SetPosition(bm.GetPosition(), true, null, false);
            }
        } // OnSelectedBookmarkChanged

        private void OnSelectionChanged(object sender, FileListingControl.SelectionChangedEventArgs e)
        {
            UpdateStatus();
        } // OnSelectionChanged

        private async void OnPositionChanged(object sender)
        {
            if (m_PositionTextBox.Text.EndsWith(m_FileManager.GetPathSeparator())) {
                try {
                    var dirs = await m_FileManager.GetDirectoryList(m_PositionTextBox.Text);
                    m_PositionTextBox.ItemsSource = dirs.OrderBy(f => f.FullPath);
                } catch {
                    m_PositionTextBox.ItemsSource = null;
                }
            }
        } // OnPositionChanged

        private void OnFilterChanged(object sender)
        {
            var ft = (TextBox)sender;

            if (m_FilterTimer != null)
                m_FilterTimer.Stop();

            if (string.IsNullOrEmpty(ft.Text)) {
                if (m_UnfilteredFiles != null) {
                    ApplyFilter(ft.Text);
                    ft.IsVisible = false;
                }
            } else {
                ft.IsVisible = true;
                // Check regex
                if (AppSettings.FilterType == Utils.Settings.FilterTypes.Regex) {
                    try {
                        var test = new Regex(ft.Text);
                        ft.Classes.Remove("error");
                    } catch {
                        ft.Classes.Add("error");
                        return;
                    }
                }
                if (m_FilterTimer == null) {
                    m_FilterTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal,
                    (sender, args) => {
                        m_FilterTimer.Stop();
                        ApplyFilter(ft.Text);
                    });
                }
                m_FilterTimer.Start();
            }
        } // OnFilterChanged

        private void OnAddBookmarkClicked(object sender, RoutedEventArgs args)
        {
            var b = GetBookmark();
            if (b != null) {
                AppSettings.Bookmarks.Add(b);
                AppSettings.Save(App.SettingsPath);
                m_BookmarksList.ItemsSource = null;
                m_BookmarksList.ItemsSource = AppSettings.Bookmarks.OrderBy(b => b.Name);
            }
        } // OnAddBookmarkClicked
        #endregion

        #region File operations
        private async void DeleteFiles(object sender, FileListingControl.FileTriggeredEventArgs args)
        {
            StringBuilder sb = new StringBuilder();
            if (AppSettings.ConfirmDelete) {
                sb.AppendLine(Localizer.Localizer.Instance["ConfirmDelete"]);
                sb.AppendLine();
                var idx = 0;
                foreach (var file in args.Files) {
                    sb.AppendLine(file.Name);
                    idx++;
                    if (idx >= 10) {
                        sb.AppendLine("...");
                        break;
                    }
                }
            }

            if (!AppSettings.ConfirmDelete || await Views.MessageWindow.ShowConfirmMessage((Window)this.VisualRoot, Localizer.Localizer.Instance["Confirm"], sb.ToString())) {
                var dlg = new Views.FileOperationWindow();
                dlg.SetFileDelete(m_FileManager, args.Files);
                var task = dlg.ShowDialog((Window)this.VisualRoot);
                dlg.Start();
                await task;
                await Refresh();
            }
        } // DeleteFiles

        public async void CreateDirectory()
        {
            var name = await Views.InputTextWindow.AskInputText((Window)this.VisualRoot, Localizer.Localizer.Instance["NewDirectory"], Localizer.Localizer.Instance["Name"]);
            if (!string.IsNullOrEmpty(name)) {
                try {
                    var newDir = System.IO.Path.Combine(Position, name);
                    if (await m_FileManager.CreateDirectory(newDir))
                        await Refresh(newDir);
                } catch (Exception ex) {
                    await ShowError(null, ex);
                }
            }
        } // CreateDirectory

        public async void RenameFile(FileManagers.FileInfo file)
        {
            var newName = await Views.InputTextWindow.AskInputText((Window)this.VisualRoot, Localizer.Localizer.Instance["Rename"], Localizer.Localizer.Instance["Name"], file.Name);
            if (!string.IsNullOrEmpty(newName)) {
                try {
                    var newFullName = System.IO.Path.Combine(Position, newName);
                    await m_FileManager.Rename(file.FullPath, newFullName);
                    await Refresh(newFullName);
                }
                catch (Exception ex) {
                    await ShowError(null, ex);
                }
            }
        } // RenameFile

        public FileManagers.FileManagerBase.Bookmark GetBookmark()
        {
            if (!string.IsNullOrEmpty(this.Position) && m_FileManager != null) {
                var b = m_FileManager.GetBookmark();
                b.Name = PositionName;
                b.Position = Position;
                return b;
            }
            return null;
        } // GetBookmark
        #endregion

        private bool IsFilterKey(bool isFocused, KeyEventArgs args)
        {
            if (args.KeyModifiers == KeyModifiers.Control || args.KeyModifiers == KeyModifiers.Alt)
                return false;

            if (args.Key.ToString().Length == 1)
                return true;

            if (isFocused) {
                HashSet<Key> nfk = new HashSet<Key>() { Key.Left, Key.Right, Key.Home, Key.End, Key.Cancel, Key.Back, Key.Space,
                                                        Key.Escape };
                if (nfk.Contains(args.Key))
                    return true;
            }
            return false;
        } // IsFilterKey

        private async Task<bool> Refresh(string selectedFile = null)
        {
            m_FileListing.SetFiles(new List<FileManagers.FileInfo>());

            m_FilterTextbox.Text = string.Empty;
            m_Status.Text = Localizer.Localizer.Instance["Loading"];
            m_FileListing.ShowWait = true;

            try {
                if (m_RefreshCancellationToken != null)
                    m_RefreshCancellationToken.Cancel();

                await m_RefreshSema.WaitAsync();
                if (m_RefreshCancellationToken != null)
                    m_RefreshCancellationToken.Dispose();
                m_RefreshCancellationToken = new CancellationTokenSource();

                m_DriveInfo = await m_FileManager.GetDriveInfo(Position);
                var files = await m_FileManager.GetFileList(Position, m_RefreshCancellationToken.Token, ShowHiddenFiles);
                if (!m_RefreshCancellationToken.IsCancellationRequested) {
                    m_FileListing.SetFiles(files, selectedFile);
                    UpdateStatus();
                }
            } catch (Exception ex) {
                m_Status.Text = string.Empty;
                await ShowError(null, ex);
            } finally {
                m_RefreshSema.Release();
                m_GoUp.IsEnabled = Files.Where(f => f.Name == "..").FirstOrDefault() != null;
                m_FileListing.ShowWait = false;
            }
            return true;
        } // Refresh

        private void ApplyFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter)) {
                m_FileListing.SetFiles(m_UnfilteredFiles, m_FileListing.CurrentRow?.FullPath);
                m_UnfilteredFiles = null;
            } else {
                if (m_UnfilteredFiles == null)
                    m_UnfilteredFiles = new List<FileManagers.FileInfo>(Files);

                var type = AppSettings.FilterType;
                switch (type) {
                    case Utils.Settings.FilterTypes.Contains:
                        m_FileListing.SetFiles(m_UnfilteredFiles.Where(f => f.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase)));
                        break;
                    case Utils.Settings.FilterTypes.StartsWith:
                        m_FileListing.SetFiles(m_UnfilteredFiles.Where(f => f.Name.StartsWith(filter, StringComparison.InvariantCultureIgnoreCase)));
                        break;
                    case Utils.Settings.FilterTypes.Regex:
                        var regex = new Regex(filter);
                        m_FileListing.SetFiles(m_UnfilteredFiles.Where(f => regex.Match(f.Name).Success));
                        break;
                }
            }
            UpdateStatus();
        } // ApplyFilter

        private void UpdateStatus()
        {
            if (m_FileListing.SelectedFiles.Count > 0) {
                long totalSize = 0;
                foreach (var si in m_FileListing.SelectedFiles)
                    totalSize += (si as FileManagers.FileInfo).Size;
                m_Status.Text = $"{m_FileListing.SelectedFiles.Count} items selected: {Utils.Utils.FormatSize(totalSize)}";
            } else {
                m_Status.Text = $"{Files.Count} items: {Utils.Utils.FormatSize(Files.Sum(f => f.Size))}";
            }

            if (m_DriveInfo != null) {
                m_Status.Text = $"{m_Status.Text}, free space {Utils.Utils.FormatSize(m_DriveInfo.FreeSpace)} ({m_DriveInfo.Format})";
            }
        } // UpdateStatus

        private async Task<bool> ShowError(string message, Exception ex)
        {
            return await Views.MessageWindow.ShowException((Window)this.VisualRoot, message, ex);
        } // ShowError
    }
}