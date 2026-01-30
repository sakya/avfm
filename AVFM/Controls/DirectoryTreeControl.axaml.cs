using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using System.Linq;

namespace AVFM.Controls
{
    public partial class DirectoryTreeControl : UserControl
    {
        class TreeItem : Utils.ContextBase
        {
            private bool m_Expanded = false;
            public TreeItem(FileManagers.FileInfo file)
            {
                File = file;
                Children = new ObservableCollection<TreeItem>();
            }

            public FileManagers.FileInfo File { get; set; }
            public string Icon { get; set; }
            public ObservableCollection<TreeItem> Children { get; set;}
            public bool IsExpanded {
                get { return m_Expanded; }
                set { SetIfChanged(ref m_Expanded, value); }
            }

            public bool Loaded { get; set; }
        } // TreeItem

        private string m_SelectedPath = null;
        private Controls.FileManagerControl m_FileManagerControl = null;
        private FileManagers.FileManagerBase m_FileManager = null;
        private ObservableCollection<TreeItem> m_Items = new ObservableCollection<TreeItem>();
        private bool m_LoadingItems = false;

        public DirectoryTreeControl()
        {
            InitializeComponent();

            m_Tree.ItemsSource = m_Items;
            m_Tree.SelectionChanged += OnSelectionChanged;
        } // DirectoryTreeControl

        public string Root
        {
            get;
            private set;
        }

        private Utils.Settings AppSettings
        {
            get { return ((App)App.Current).Settings; }
        }

        private StringComparison StringComparison
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    return StringComparison.InvariantCultureIgnoreCase;
                return StringComparison.InvariantCulture;
            }
        }

        public string SelectedPath
        {
            get {
                var node = m_Tree.SelectedItem as TreeItem;
                if (node != null)
                    return node.File.FullPath;
                return null;
            }
        }

        public async Task<bool> Set(Controls.FileManagerControl fileManagerControl, string position)
        {
            if (!this.IsVisible)
                return false;

            m_FileManagerControl = fileManagerControl;
            if (m_FileManager != fileManagerControl.FileManager) {
                m_FileManager = fileManagerControl.FileManager;
                m_Items.Clear();

                var drives = (await m_FileManager.GetDrives()).OrderBy(d => d.Name);
                foreach (var drive in drives) {
                    Root = m_FileManager.GetRoot();
                    var root = new FileManagers.FileInfo()
                    {
                        Name = string.IsNullOrEmpty(drive.VolumeLabel) ? drive.Name : $"{drive.VolumeLabel} ({drive.Name})",
                        FullPath = drive.Name,
                        IsDirectory = true,
                        Type = FileManagers.FileManagerBase.DirectoryMimeType
                    };

                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        root.Type = FileManagers.FileManagerBase.WindowsDriveMimeType;
                    m_Items.Add(await GetTreeItem(root));
                }
            }
            return await SelectDirectory(position);
        } // Set

        public async Task<bool> SelectDirectory(string position)
        {
            if (!position.EndsWith(m_FileManager.GetPathSeparator()))
                position = $"{position}{m_FileManager.GetPathSeparator()}";
            m_SelectedPath = position;

            var drive = await m_FileManager.GetDriveInfo(position);
            List<string> paths = new List<string>() { drive.Name };
            int idx = position.IndexOf(m_FileManager.GetPathSeparator(), drive.Name.Length);
            while (idx >= 0) {
                var path = position.Substring(0, idx);
                if (!paths.Contains(path))
                    paths.Add(position.Substring(0, idx));
                idx = position.IndexOf(m_FileManager.GetPathSeparator(), idx + 1);
            }

            // Expand the tree
            m_Tree.SelectedItem = null;
            var items = m_Items;
            idx = 0;
            foreach (var path in paths) {
                var node = items
                    .FirstOrDefault(i => string.Compare(i.File.FullPath, path, StringComparison) == 0);
                if (node != null) {
                    node.IsExpanded = true;
                    if (idx == paths.Count - 1 && m_SelectedPath == position)
                        m_Tree.SelectedItem = node;
                    while (m_LoadingItems) {
                        await Task.Delay(10);
                    }
                    items = node.Children;
                }
                idx++;
            }
            return true;
        } // SelectDirectory

        private async Task<TreeItem> GetTreeItem(FileManagers.FileInfo fi)
        {
            var ti = new TreeItem(fi);
            ti.Icon = await m_FileManager.GetMimeIcon(fi.Type, fi.FullPath);
            try {
                if (await m_FileManager.HasSubdirs(fi.FullPath)) {
                    ti.Children.Add(new TreeItem(null));

                    ti.PropertyChanged += async (sender, args) => {
                        if (args.PropertyName == "IsExpanded") {
                            var sti = sender as TreeItem;
                            if (sti.IsExpanded && !sti.Loaded) {
                                m_LoadingItems = true;
                                m_Tree.Cursor = new Cursor(StandardCursorType.Wait);
                                sti.Children.Clear();
                                var dirs = (await m_FileManager.GetDirectoryList(sti.File.FullPath, AppSettings.ShowHiddenFiles)).OrderBy(d => d.Name);
                                foreach (var dir in dirs) {
                                    sti.Children.Add(await GetTreeItem(dir));
                                }
                                m_Tree.Cursor = new Cursor(StandardCursorType.Arrow);
                                sti.Loaded = true;
                                m_LoadingItems = false;
                            }
                        }
                    };
                }
            } catch {

            }
            return ti;
        } // GetTreeItem

        private async void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (args.AddedItems.Count > 0) {
                var ti = args.AddedItems[0] as TreeItem;
                if (m_FileManagerControl.Position != ti.File.FullPath) {
                    await m_FileManagerControl.SetPosition(ti.File.FullPath);
                }
            }
        } // OnSelectionChanged
    }
}