using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using System.Linq;
using Avalonia.Media;

namespace AVFM.Controls
{
    public partial class FileListingControl : UserControl
    {
        #region Comparers
        class NameComparer : IComparer<FileItem>
        {
            SortInfo m_SortInfo = null;
            public NameComparer(SortInfo sortInfo)
            {
                m_SortInfo = sortInfo;
            }

            public int Compare(FileItem first, FileItem second)
            {
                bool descending = m_SortInfo != null ? m_SortInfo.Order == SortInfo.SortOrder.Descending : false;
                if (first == null || second == null)
                    return 0;

                if (((App)App.Current).Settings.SortFolderBeforeFiles) {
                    if (first.File.IsDirectory && !second.File.IsDirectory)
                        return descending ? 1 : -1;
                    else if (second.File.IsDirectory && !first.File.IsDirectory)
                        return descending ? -1 : 1;
                }
                return string.Compare(first.File.Name, second.File.Name, System.StringComparison.InvariantCultureIgnoreCase);
            }
        } // NameComparer

        class SizeComparer : IComparer<FileItem>
        {
            SortInfo m_SortInfo = null;
            public SizeComparer(SortInfo sortInfo)
            {
                m_SortInfo = sortInfo;
            }

            public int Compare(FileItem first, FileItem second)
            {
                bool descending = m_SortInfo != null ? m_SortInfo.Order == SortInfo.SortOrder.Descending : false;
                if (first == null || second == null)
                    return 0;

                if (((App)App.Current).Settings.SortFolderBeforeFiles) {
                    if (first.File.IsDirectory && !second.File.IsDirectory)
                        return descending ? 1 : -1;
                    else if (second.File.IsDirectory && !first.File.IsDirectory)
                        return descending ? -1 : 1;
                }
                return first.File.Size.CompareTo(second.File.Size);
            }
        } // SizeComparer

        class TypeComparer : IComparer<FileItem>
        {
            SortInfo m_SortInfo = null;
            public TypeComparer(SortInfo sortInfo)
            {
                m_SortInfo = sortInfo;
            }

            public int Compare(FileItem first, FileItem second)
            {
                bool descending = m_SortInfo != null ? m_SortInfo.Order == SortInfo.SortOrder.Descending : false;
                if (first == null || second == null)
                    return 0;

                if (((App)App.Current).Settings.SortFolderBeforeFiles) {
                    if (first.File.IsDirectory && !second.File.IsDirectory)
                        return descending ? 1 : -1;
                    else if (second.File.IsDirectory && !first.File.IsDirectory)
                        return descending ? -1 : 1;
                }
                return string.Compare(first.File.Type, second.File.Type, System.StringComparison.InvariantCultureIgnoreCase);
            }
        } // TypeComparer

        class LastModifiedComparer : IComparer<FileItem>
        {
            SortInfo m_SortInfo = null;
            public LastModifiedComparer(SortInfo sortInfo)
            {
                m_SortInfo = sortInfo;
            }
            public int Compare(FileItem first, FileItem second)
            {
                bool descending = m_SortInfo != null ? m_SortInfo.Order == SortInfo.SortOrder.Descending : false;
                if (first == null || second == null)
                    return 0;

                if (((App)App.Current).Settings.SortFolderBeforeFiles) {
                    if (first.File.IsDirectory && !second.File.IsDirectory)
                        return descending ? 1 : -1;
                    else if (second.File.IsDirectory && !first.File.IsDirectory)
                        return descending ? -1 : 1;
                }
                return first.File.LastModified.CompareTo(second.File.LastModified);
            }
        }
        #endregion

        #region Events
        public class FileTriggeredEventArgs : EventArgs
        {
            public FileTriggeredEventArgs(List<FileManagers.FileInfo> file)
            {
                Files = file;
            }

            public List<FileManagers.FileInfo> Files { get; set; }
        }
        public delegate void FileTriggeredHandler(object sender, FileTriggeredEventArgs e);
        public event FileTriggeredHandler FileTriggered;
        public event FileTriggeredHandler FileDeleteRequest;
        public event FileTriggeredHandler FileCopyRequest;
        public event FileTriggeredHandler FileMoveRequest;

        public class SelectionChangedEventArgs : EventArgs
        {
            public SelectionChangedEventArgs(List<FileManagers.FileInfo> selectedFiles)
            {
                SelectedFiles = selectedFiles;
            }

            public List<FileManagers.FileInfo> SelectedFiles { get; set; }
        }
        public delegate void SelectionChangedHandler(object sender, SelectionChangedEventArgs e);
        public event SelectionChangedHandler SelectionChanged;

        #endregion

        public enum Columns {
            Name,
            Size,
            Type,
            LastModified
        }

        public enum ViewModes
        {
            List,
            Icon
        }

        public class SortInfo {
            public enum SortOrder {
                Ascending,
                Descending
            }

            public Columns Column { get; set;}
            public SortOrder Order { get; set; }
        }

        class Context : Utils.ContextBase
        {
            private GridLength m_NameWidth;
            private GridLength m_SizeWidth;
            private GridLength m_TypeWidth;
            private GridLength m_LastModifiedWidth;

            public GridLength NameWidth
            {
                get { return m_NameWidth; }
                set { SetIfChanged(ref m_NameWidth, value); }
            }

            public GridLength SizeWidth
            {
                get { return m_SizeWidth; }
                set { SetIfChanged(ref m_SizeWidth, value); }
            }

            public GridLength TypeWidth
            {
                get { return m_TypeWidth; }
                set { SetIfChanged(ref m_TypeWidth, value); }
            }

            public GridLength LastModifiedWidth
            {
                get { return m_LastModifiedWidth; }
                set { SetIfChanged(ref m_LastModifiedWidth, value); }
            }
        } // Context

        class FileItem : Utils.ContextBase
        {
            private SolidColorBrush m_Background = new SolidColorBrush(Colors.Transparent);
            private SolidColorBrush m_SecondaryBackground = new SolidColorBrush(Colors.Transparent);
            private string m_FileMimeType;
            private string m_Icon;
            public FileItem(FileManagers.FileInfo file) {
                File = file;
            }
            public FileManagers.FileInfo File { get; set; }

            public string FileMimeType {
                get { return m_FileMimeType; }
                set { SetIfChanged(ref m_FileMimeType, value); }
            }

            public string Icon {
                get { return m_Icon; }
                set { SetIfChanged(ref m_Icon, value); }
            }

            public SolidColorBrush Background {
                get { return m_Background; }
                set { SetIfChanged(ref m_Background, value); }
            }

            public SolidColorBrush SecondaryBackground
            {
                get { return m_SecondaryBackground; }
                set { SetIfChanged(ref m_SecondaryBackground, value); }
            }
        }

        private Context m_Context = new Context();
        private List<FileItem> m_Files = null;
        private FileItem m_CurrentRow = null;
        private List<FileManagers.FileInfo> m_SelectedFiles = new List<FileManagers.FileInfo>();
        private List<Border> m_RowBackgrounds = new List<Border>();
        private List<Border> m_RowForegrounds = new List<Border>();
        private Border m_HeaderPressed = null;
        private FileItem m_FilePressed = null;
        private SortInfo m_SortInfo = new SortInfo() { Column = Columns.Name, Order = SortInfo.SortOrder.Ascending };
        private bool m_ShowWait = false;
        private ViewModes m_ViewMode = ViewModes.List;

        public FileListingControl()
        {
            InitializeComponent();

            m_MainGrid.DataContext = m_Context;

            m_Context.NameWidth = m_HeaderGrid.ColumnDefinitions[0].Width;
            m_Context.SizeWidth = m_HeaderGrid.ColumnDefinitions[2].Width;
            m_Context.TypeWidth = m_HeaderGrid.ColumnDefinitions[4].Width;
            m_Context.LastModifiedWidth = m_HeaderGrid.ColumnDefinitions[6].Width;

            foreach (var cd in m_HeaderGrid.ColumnDefinitions) {
                cd.PropertyChanged += (sender, args) => {
                    if (args.Property.Name == "Width") {
                        var idx = m_HeaderGrid.ColumnDefinitions.IndexOf(sender as ColumnDefinition);
                        switch (idx) {
                            case 0:
                                m_Context.NameWidth = (GridLength)args.NewValue;
                                break;
                            case 2:
                                m_Context.SizeWidth = (GridLength)args.NewValue;
                                break;
                            case 4:
                                m_Context.TypeWidth = (GridLength)args.NewValue;
                                break;
                            case 6:
                                m_Context.LastModifiedWidth = (GridLength)args.NewValue;
                                break;
                        }
                    }
                };
            }
        }

        public FileManagers.FileManagerBase FileManager { get; set; }
        public List<FileManagers.FileInfo> Files {
            get {
                var res = new List<FileManagers.FileInfo>();
                foreach (var f in m_Files)
                    res.Add(f.File);
                return res;
             }
        }

        public FileManagers.FileInfo CurrentRow {
            get { return m_CurrentRow?.File; }
        }

        public List<FileManagers.FileInfo> SelectedFiles {
            get { return m_SelectedFiles; }
        }

        public bool ShowWait {
            get { return m_ShowWait; }
            set {
                m_ShowWait = value;
                m_Wait.IsVisible = m_ShowWait;
                if (m_ShowWait)
                    m_Wait.Classes.Add("spinner");
                else
                    m_Wait.Classes.Remove("spinner");
            }
        }

        public ViewModes ViewMode {
            get { return m_ViewMode; }
            set {
                if (m_ViewMode != value) {
                    m_ScrollViewer.IsVisible = value == ViewModes.List;
                    m_HeaderGrid.IsVisible = value == ViewModes.List;
                    m_IconScrollViewer.IsVisible = value == ViewModes.Icon;
                    m_ViewMode = value;
                }
            }
        }

        public SolidColorBrush OverRowColor { get; set; } = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));
        public SolidColorBrush CurrentRowColor { get; set; } = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
        public SolidColorBrush SelectedRowColor { get; set; } = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));

        private Utils.Settings AppSettings
        {
            get { return ((App)App.Current).Settings; }
        }

        private void OnHeaderPointerEnter(object sender, PointerEventArgs args)
        {
            var b =(Border)sender;
            b.Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));
        }

        private void OnHeaderPointerLeave(object sender, PointerEventArgs args)
        {
            var b = (Border)sender;
            b.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void OnHeaderPointerPressed(object sender, PointerPressedEventArgs args)
        {
            m_HeaderPressed = (Border)sender;
        } // OnHeaderPointerPressed

        private async void OnHeaderPointerReleased(object sender, PointerReleasedEventArgs args)
        {
            var b = (Border)sender;
            if (m_HeaderPressed == b) {
                var col = Enum.Parse<Columns>((string)b.Tag);
                if (m_SortInfo == null) {
                    m_SortInfo = new SortInfo() {
                        Order = SortInfo.SortOrder.Ascending
                    };
                } else if (m_SortInfo.Column == col) {
                    m_SortInfo.Order = m_SortInfo.Order == SortInfo.SortOrder.Ascending ? SortInfo.SortOrder.Descending : SortInfo.SortOrder.Ascending;
                }
                m_SortInfo.Column = col;
                await Task.Run( () => SortFiles());
                RenderFiles(m_CurrentRow?.File.FullPath);
            }
            m_HeaderPressed = null;
        } // OnHeaderPointerReleased

        private void OnFilePointerEnter(object sender, PointerEventArgs args)
        {
            var b = (Border)sender;
            var file = b.DataContext as FileItem;
            if (file != null && file != m_CurrentRow)
                file.SecondaryBackground = OverRowColor;
        } // OnFilePointerEnter

        private void OnFilePointerLeave(object sender, PointerEventArgs args)
        {
            var b = (Border)sender;
            var file = b.DataContext as FileItem;
            if (file != null && file != m_CurrentRow)
                file.SecondaryBackground = new SolidColorBrush(Colors.Transparent);
        } // OnFilePointerLeave

        private void OnFilePointerPressed(object sender, PointerPressedEventArgs args)
        {
            m_FilePressed = ((Border)sender).DataContext as FileItem;
        } // OnFilePointerPressed

        private void OnFilePointerReleased(object sender, PointerReleasedEventArgs args)
        {
            var b = (Border)sender;
            if (m_FilePressed == b.DataContext as FileItem) {
                var file = b.DataContext as FileItem;
                if (file != null) {
                    if (m_CurrentRow != null)
                        m_CurrentRow.SecondaryBackground = new SolidColorBrush(Colors.Transparent);
                    m_CurrentRow = file;
                    if (m_CurrentRow != null)
                        m_CurrentRow.SecondaryBackground = CurrentRowColor;

                    if (args.InitialPressMouseButton == MouseButton.Right && AppSettings.SelectFileWithRightClick) {
                        SelectCurrentRowFile();
                    }
                }
            }
            m_FilePressed = null;
        } // OnFilePointerReleased

        private void OnFileDoubleTapped(object sender, TappedEventArgs args)
        {
            args.Handled = true;
            var b = (Border)sender;
            var file = b.DataContext as FileItem;

            FileTriggered?.Invoke(this, new FileTriggeredEventArgs(new List<FileManagers.FileInfo>() { file.File }));
        } // OnFileDoubleTapped

        private async void OnFilePrepared(object sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            var fi = args.Element.DataContext as FileItem;
            if (fi.File.Type == null) {
                fi.File.Type = await Task.Run( () => FileManager.GetFileMimeType(fi.File.FullPath));
                fi.FileMimeType = fi.File.Type;
            } else {
                fi.FileMimeType = fi.File.Type;
            }

            if (fi.File.Icon == null && !string.IsNullOrEmpty(fi.File.Type)) {
                fi.File.Icon = await FileManager.GetMimeIcon(fi.File.Type, fi.File.FullPath);
                fi.Icon = fi.File.Icon;
            } else {
                fi.Icon = fi.File.Icon;
            }
        } // OnFilePrepared

        private async void OnIconAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs args)
        {
            var fi = (sender as Border).DataContext as FileItem;
            if (fi.File.Type == null) {
                fi.File.Type = await Task.Run( () => FileManager.GetFileMimeType(fi.File.FullPath));
                fi.FileMimeType = fi.File.Type;
            } else {
                fi.FileMimeType = fi.File.Type;
            }

            if (fi.File.Icon == null && !string.IsNullOrEmpty(fi.File.Type)) {
                fi.File.Icon = await FileManager.GetMimeIcon(fi.File.Type, fi.File.FullPath);
                fi.Icon = fi.File.Icon;
            } else {
                fi.Icon = fi.File.Icon;
            }
        } // OnIconAttachedToVisualTree

        private void SelectCurrentRowFile()
        {
            if (m_CurrentRow != null && !m_CurrentRow.File.IsFakeDirectory) {
                if (m_SelectedFiles.Contains(m_CurrentRow.File)) {
                    m_SelectedFiles.Remove(m_CurrentRow.File);
                    m_CurrentRow.Background = new SolidColorBrush(Colors.Transparent);
                } else {
                    m_SelectedFiles.Add(m_CurrentRow.File);
                    m_CurrentRow.Background = SelectedRowColor;
                }
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(m_SelectedFiles));
            }
        } // SelectCurrentRowFile

        public void ManageKeyDown(KeyEventArgs args)
        {
            var currRowIndex = m_Files.IndexOf(m_CurrentRow);
            switch (args.Key) {
                case Key.Up:
                case Key.FnUpArrow:
                    args.Handled = true;
                    if (currRowIndex > 0) {
                        if (args.KeyModifiers == KeyModifiers.Shift) {
                            SelectRow(m_CurrentRow, true);
                        }
                        currRowIndex--;
                        SetCurrentRow(currRowIndex);
                    }
                    break;
                case Key.Down:
                case Key.FnDownArrow:
                    args.Handled = true;
                    if (currRowIndex + 1 < m_Files.Count) {
                        if (args.KeyModifiers == KeyModifiers.Shift) {
                            SelectRow(m_CurrentRow, true);
                        }
                        currRowIndex++;
                        SetCurrentRow(currRowIndex);
                    }
                    break;
                case Key.PageUp:
                    args.Handled = true;
                    if (currRowIndex > 0) {
                        if (args.KeyModifiers == KeyModifiers.Shift) {
                            for (int idx = 0; idx < 10; idx++) {
                                if (currRowIndex - idx < 0)
                                    break;
                                SelectRow(m_Files[currRowIndex - idx], true);
                            }
                        }
                        currRowIndex -= 10;
                        if (currRowIndex < 0)
                            currRowIndex = 0;
                        SetCurrentRow(currRowIndex);
                    }
                    break;
                case Key.PageDown:
                    args.Handled = true;
                    if (currRowIndex + 1 < m_Files.Count) {
                        if (args.KeyModifiers == KeyModifiers.Shift) {
                            for (int idx = 0; idx < 10; idx++) {
                                if (currRowIndex + idx == m_Files.Count)
                                    break;
                                SelectRow(m_Files[currRowIndex + idx], true);
                            }
                        }
                        currRowIndex += 10;
                        if (currRowIndex + 1 >= m_Files.Count)
                            currRowIndex = m_Files.Count - 1;
                        SetCurrentRow(currRowIndex);
                    }
                    break;
                case Key.Home:
                    args.Handled = true;
                    if (currRowIndex != 0) {
                        currRowIndex = 0;
                        SetCurrentRow(currRowIndex);
                    }
                    break;
                case Key.End:
                    args.Handled = true;
                    if (currRowIndex != m_Files.Count - 1) {
                        currRowIndex = m_Files.Count - 1;
                        SetCurrentRow(currRowIndex);
                    }
                    break;
                case Key.Back:
                    args.Handled = true;
                    var up = m_Files.Where(f => f.File.Name == "..").FirstOrDefault();
                    if (up != null)
                        FileTriggered?.Invoke(this, new FileTriggeredEventArgs(new List<FileManagers.FileInfo>() { up.File }));
                    break;
                case Key.Return:
                    args.Handled = true;
                    if (m_CurrentRow != null) {
                        FileTriggered?.Invoke(this, new FileTriggeredEventArgs(new List<FileManagers.FileInfo>() { m_CurrentRow.File }));
                    }
                    break;
            }

            // Shortcuts
            if (!args.Handled) {
                var sh = AppSettings.GetShortcut(args);
                if (sh != null) {
                    switch (sh.Type) {
                        case Utils.Settings.Shortcut.Shortcuts.SelectFile:
                            SelectCurrentRowFile();
                            break;
                        case Utils.Settings.Shortcut.Shortcuts.SelectAllFiles:
                            m_SelectedFiles.Clear();
                            foreach (var file in m_Files) {
                                if (file.File.IsFakeDirectory)
                                    continue;
                                m_SelectedFiles.Add(file.File);
                                file.Background = SelectedRowColor;
                            }
                            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(m_SelectedFiles));
                            break;
                        case Utils.Settings.Shortcut.Shortcuts.CopyFile:
                            if (m_CurrentRow != null) {
                                FileTriggeredEventArgs eArgs = null;
                                if (m_SelectedFiles.Count > 0)
                                    eArgs = new FileTriggeredEventArgs(m_SelectedFiles);
                                else
                                    eArgs = new FileTriggeredEventArgs(new List<FileManagers.FileInfo>() { m_CurrentRow.File });
                                FileCopyRequest?.Invoke(this, eArgs);
                            }
                            break;
                        case Utils.Settings.Shortcut.Shortcuts.MoveFile:
                            if (m_CurrentRow != null) {
                                FileTriggeredEventArgs eArgs = null;
                                if (m_SelectedFiles.Count > 0)
                                    eArgs = new FileTriggeredEventArgs(m_SelectedFiles);
                                else
                                    eArgs = new FileTriggeredEventArgs(new List<FileManagers.FileInfo>() { m_CurrentRow.File });
                                FileMoveRequest?.Invoke(this, eArgs);
                            }
                            break;
                        case Utils.Settings.Shortcut.Shortcuts.DeleteFile:
                            if (m_CurrentRow != null && m_CurrentRow.File.Name != "..")
                            {
                                FileTriggeredEventArgs eArgs = null;
                                if (m_SelectedFiles.Count > 0)
                                    eArgs = new FileTriggeredEventArgs(m_SelectedFiles);
                                else
                                    eArgs = new FileTriggeredEventArgs(new List<FileManagers.FileInfo>() { m_CurrentRow.File });
                                FileDeleteRequest?.Invoke(this, eArgs);
                            }
                            break;
                    }
                }
            }
        } // ManageKeyDown

        public void SetFiles(IEnumerable<FileManagers.FileInfo> files, string selectedFile = null)
        {
            m_Files = new List<FileItem>();
            if (files != null) {
                foreach (var f in files)
                    m_Files.Add(new FileItem(f));
            }
            m_SelectedFiles.Clear();
            SortFiles();
            RenderFiles(selectedFile);
        } // SetFiles

        private void SetCurrentRow(int index)
        {
            if (m_CurrentRow != null)
                m_CurrentRow.SecondaryBackground = new SolidColorBrush(Colors.Transparent);
            m_CurrentRow = m_Files[index];
            m_CurrentRow.SecondaryBackground = CurrentRowColor;
            BringItemIntoView(index);
        } // SetCurrentRow

        private void SelectRow(FileItem file, bool deselectIfSelected = false)
        {
            if (file.File.IsFakeDirectory)
                return;

            if (!m_SelectedFiles.Contains(file.File)) {
                m_SelectedFiles.Add(file.File);
                file.Background = SelectedRowColor;
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(m_SelectedFiles));
            } else if (deselectIfSelected) {
                m_SelectedFiles.Remove(file.File);
                file.Background = new SolidColorBrush(Colors.Transparent);
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(m_SelectedFiles));
            }
        } // SelectRow

        private void BringItemIntoView(int index)
        {
            if (index < 0)
                return;

            ItemsRepeater ir = null;
            if (m_ViewMode == ViewModes.List)
                ir = m_Items;
            else if (m_ViewMode == ViewModes.Icon)
                ir = m_WrapPanel;

            var item = ir.TryGetElement(index);
            if (item == null)
                item = ir.GetOrCreateElement(index);
            UpdateLayout();
            item.BringIntoView();
        } // BringItemIntoView

        private void RenderFiles(string selectedFile = null)
        {
            m_Items.IsHitTestVisible = false;
            m_WrapPanel.IsHitTestVisible = false;
            m_Items.ItemsSource = null;
            m_Items.ItemsSource = m_Files;
            m_WrapPanel.ItemsSource = null;
            m_WrapPanel.ItemsSource = m_Files;

            m_CurrentRow = null;
            if (selectedFile != null)
                m_CurrentRow = m_Files.Where(f => f.File.FullPath == selectedFile).FirstOrDefault();
            if (m_CurrentRow == null)
                m_CurrentRow = m_Files.Count > 0 ? m_Files[0] : null;

            // Sort indicator
            foreach (var g in m_HeaderGrid.Children) {
                var bh = g as Border;
                if (bh != null) {
                    var gh = bh.Child as Grid;
                    if (gh?.Children.Count == 2)
                        gh.Children.RemoveAt(1);
                }
            }

            if (m_SortInfo != null) {
                var icon = new Projektanker.Icons.Avalonia.Icon()
                {
                    Value = m_SortInfo.Order == SortInfo.SortOrder.Ascending ? "fas fa-arrow-down" : "fas fa-arrow-up",
                    FontSize = 12
                };
                var bh = GetHeader(m_SortInfo.Column);
                var gh = bh.Child as Grid;

                Grid.SetColumn(icon, 1);
                gh.Children.Add(icon);
            }

            if (m_CurrentRow != null) {
                m_CurrentRow.SecondaryBackground = CurrentRowColor;
                BringItemIntoView(m_Files.IndexOf(m_CurrentRow));
            }

            m_Items.IsHitTestVisible = true;
            m_WrapPanel.IsHitTestVisible = true;
        } // RenderFiles

        private void SortFiles()
        {
            if (m_SortInfo == null)
                return;

            IComparer<FileItem> comparer =null;
            switch (m_SortInfo.Column) {
                case Columns.Name:
                    comparer = new NameComparer(m_SortInfo);
                    break;
                case Columns.Size:
                    comparer = new SizeComparer(m_SortInfo);
                    break;
                case Columns.Type:
                    comparer = new TypeComparer(m_SortInfo);
                    break;
                case Columns.LastModified:
                    comparer = new LastModifiedComparer(m_SortInfo);
                    break;
            }
            m_Files.Sort(comparer);
            if (m_SortInfo.Order == SortInfo.SortOrder.Descending)
                m_Files.Reverse();
        } // SortFiles

        private Border GetHeader(Columns column)
        {
            foreach (var g in m_HeaderGrid.Children) {
                var bh = g as Border;
                if (bh != null && (string)bh.Tag == column.ToString()) {
                    return bh;
                }
            }
            return null;
        } // GetHeader
    }
}