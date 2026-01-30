using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Notifications;
using AVFM.Controls;
using AVFM.Models;

namespace AVFM.Views
{
    public partial class MainWindow : Window
    {
        public class MenuContext
        {
            public KeyGesture NewTabGesture { get; set; }
            public KeyGesture CloseTabGesture { get; set; }
            public KeyGesture CreateFolderGesture { get; set; }
        }

        private FileManagerControl m_ActiveFileManager = null;
        private readonly WindowNotificationManager m_NotificationManager = null;
        private TabItem m_ClickedTab = null;

        public MainWindow()
        {
            InitializeComponent();
            App.SetWindowTitle(this);

            Closing += OnWindowClosing;

            m_MainMenu.DataContext = new MenuContext() {
                NewTabGesture = AppSettings.GetShortcut(Utils.Settings.Shortcut.Shortcuts.NewTab)?.InputGesture,
                CloseTabGesture = AppSettings.GetShortcut(Utils.Settings.Shortcut.Shortcuts.CloseTab)?.InputGesture,
                CreateFolderGesture = AppSettings.GetShortcut(Utils.Settings.Shortcut.Shortcuts.MakeDir)?.InputGesture,
            };

            m_InnerGrid.AttachedToVisualTree += async (sender, args) =>
            {
                var tasks = new List<Task>();
                if (AppSettings.SaveOpenedTabsOnExit && AppSettings.OpenedTabs?.Count > 0) {
                    foreach (var t in AppSettings.OpenedTabs) {
                        tasks.Add(AddNewTab( t.TabPosition == Utils.Settings.OpenedTab.TabPositions.Left ? m_LeftTabControl : m_RightTabControl, t.Position, t.ViewMode));
                    }
                } else {
                    tasks.Add(AddNewTab(m_LeftTabControl, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
                    tasks.Add(AddNewTab(m_RightTabControl, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
                }
                await Task.WhenAll(tasks.ToArray());
                SetShowHiddenFiles(AppSettings.ShowHiddenFiles);
            };

            SetTreeVisibility(AppSettings.ShowTree);

            m_NotificationManager = new WindowNotificationManager(this)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 3,
                Margin = Environment.OSVersion.Platform == PlatformID.Win32NT ? new Thickness(0, 30, 0, 0) : new Thickness(0)
            };
        }

        private Utils.Settings AppSettings
        {
            get { return ((App)App.Current).Settings; }
        }

        protected override async void OnKeyDown(Avalonia.Input.KeyEventArgs args)
        {
            // Tab
            if (args.Key == Key.Tab) {
                args.Handled = true;
                var tabControl = Utils.Utils.FindParent<TabControl>(m_ActiveFileManager);
                if (tabControl == m_LeftTabControl) {
                    var ti = m_RightTabControl.SelectedItem as TabItem;
                    var fm = ti.Content as FileManagerControl;
                    fm.IsActive = true;
                } else if (tabControl == m_RightTabControl) {
                    var ti = m_LeftTabControl.SelectedItem as TabItem;
                    var fm = ti.Content as FileManagerControl;
                    fm.IsActive = true;
                }
            } else {
                // Shortcuts
                var sh = AppSettings.GetShortcut(args);
                if (sh != null) {
                    switch (sh.Type) {
                        case Utils.Settings.Shortcut.Shortcuts.NewTab:
                            args.Handled = true;
                            await AddNewTab(m_ActiveFileManager.Parent.Parent as TabControl, m_ActiveFileManager.Position);
                            break;
                        case Utils.Settings.Shortcut.Shortcuts.CloseTab:
                            args.Handled = true;
                            RemoveTab(Utils.Utils.FindParent<TabItem>(m_ActiveFileManager));
                            break;
                    }
                }
            }

            if (!args.Handled) {
                if(m_ActiveFileManager != null) {
                    m_ActiveFileManager.ManageKeyDown(args);
                }
            }
        } // OnKeyDown

        private async Task<bool> AddNewTab(
            TabControl tabControl,
            string position,
            FileListingControl.ViewModes viewMode = FileListingControl.ViewModes.List)
        {
            if (tabControl.ItemsSource == null)
                tabControl.ItemsSource = new Avalonia.Collections.AvaloniaList<object>();
            var items = tabControl.ItemsSource as Avalonia.Collections.AvaloniaList<object>;

            var fm = new FileManagerControl();
            fm.ViewMode = viewMode;
            fm.ShowHiddenFiles = AppSettings.ShowHiddenFiles;
            fm.Tag = position;
            fm.AttachedToVisualTree += OnFileManagerAttachedToVisualTree;
            fm.GotFocus += OnFileManagerGotFocus;
            fm.IsActiveChanged += (sender, args) => {
                if ((sender as FileManagerControl).IsActive)
                    OnFileManagerGotFocus(sender, null);
            };
            fm.PositionChanged += async (sender, args) => {
                var sfm = sender as FileManagerControl;
                (sfm.Parent as TabItem).Header = new TabItemHeader(sfm.PositionName, sfm.GetBookmark().Icon);
                await m_Tree.Set(sfm, args.NewPosition);
            };
            fm.FileCopyRequest += OnFileCopyRequest;
            fm.FileMoveRequest += OnFileMoveRequest;
            m_ActiveFileManager = null;

            string positionHeader = null;
            string icon = null;
            try {
                using (var tFm = FileManagers.FileManagerFactory.GetFileManager(position, App.DefaultFsFileManager, out _)) {
                    var di = await tFm.GetPositionInfo(position);
                    positionHeader = di.Name;
                    icon = tFm.GetBookmark().Icon;
                }
            } catch {
                positionHeader = position;
            }

            var tabItem = new TabItem()
            {
                Header = new TabItemHeader(positionHeader, icon),
                Content = fm,
                IsSelected = true
            };
            items.Add(tabItem);

            return true;
        } // AddNewTab

        private void RemoveTab(TabItem tabItem)
        {
            var tabControl = tabItem.Parent as TabControl;

            var tabItems = tabControl.ItemsSource as Avalonia.Collections.AvaloniaList<object>;
            if (tabItems.Count > 1)
                tabItems.Remove(tabItem);
        } // RemoveTab

        private void OnTabChanged(object sender, SelectionChangedEventArgs args)
        {
            if (args.AddedItems.Count == 0)
                return;

            var ti = args.AddedItems[0] as TabItem;
            if (ti == null)
                return;

            var fm = ti.Content as FileManagerControl;
            if (fm.Tag == null) {
                fm.Tag = fm.Position;
                m_ActiveFileManager = null;
            }
        } // OnTabChanged

        private async void OnFileManagerAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs args)
        {
            var fm = sender as FileManagerControl;
            if (fm.Tag != null) {
                var initialPosition = (string)fm.Tag;
                await fm.SetPosition(initialPosition);
                if (m_ActiveFileManager == null) {
                    fm.IsActive = true;
                    m_ActiveFileManager = fm;
                }
                fm.Tag = null;
            }
        } // OnFileManagerAttachedToVisualTree

        private async void OnFileManagerGotFocus(object sender, GotFocusEventArgs args)
        {
            m_ActiveFileManager = sender as FileManagerControl;
            m_ActiveFileManager.IsActive = true;
            SetToggleIcons();
            if (m_ActiveFileManager.FileManager != null) {
                if (string.Compare(m_Tree.SelectedPath, m_ActiveFileManager.Position,
                    Environment.OSVersion.Platform == PlatformID.Win32NT ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture) != 0) {
                    await m_Tree.Set(m_ActiveFileManager, m_ActiveFileManager.Position);
                }
            }
        } // OnFileManagerGotFocus

        private async void OnFileCopyRequest(object sender, FileListingControl.FileTriggeredEventArgs args)
        {
            var sourceCtrl = sender as FileManagerControl;
            var tc = Utils.Utils.FindParent<TabControl>(sourceCtrl);
            TabControl otherTc = m_LeftTabControl;
            if (tc == m_LeftTabControl)
                otherTc = m_RightTabControl;

            var destCtrl = (otherTc.SelectedItem as TabItem).Content as FileManagerControl;
            var sourceFm = sourceCtrl.FileManager;
            var destFm = destCtrl.FileManager;

            if (!AppSettings.ConfirmCopy || await Views.MessageWindow.ShowConfirmMessage((Window)VisualRoot, Localizer.Localizer.Instance["Confirm"], string.Format(Localizer.Localizer.Instance["ConfirmCopy"], destCtrl.Position))) {
                var dlg = new Views.FileOperationWindow();
                dlg.SetFileCopy(sourceCtrl.Position, sourceFm, destCtrl.Position, destFm, args.Files);
                var task = dlg.ShowDialog((Window)VisualRoot);
                dlg.Start();
                await task;
                destCtrl.RefreshPosition();
            }
        } // OnFileCopyRequest

        private async void OnFileMoveRequest(object sender, FileListingControl.FileTriggeredEventArgs args)
        {
            var sourceCtrl = sender as FileManagerControl;
            var tc = Utils.Utils.FindParent<TabControl>(sourceCtrl);
            TabControl otherTc = m_LeftTabControl;
            if (tc == m_LeftTabControl)
                otherTc = m_RightTabControl;

            var destCtrl = (otherTc.SelectedItem as TabItem).Content as FileManagerControl;
            var sourceFm = sourceCtrl.FileManager;
            var destFm = destCtrl.FileManager;

            if (!AppSettings.ConfirmMove || await Views.MessageWindow.ShowConfirmMessage((Window)VisualRoot, Localizer.Localizer.Instance["Confirm"], string.Format(Localizer.Localizer.Instance["ConfirmMove"], destCtrl.Position))) {
                var dlg = new Views.FileOperationWindow();
                dlg.SetFileMove(sourceCtrl.Position, sourceFm, destFm, destCtrl.Position, args.Files);
                var task = dlg.ShowDialog((Window)VisualRoot);
                dlg.Start();
                await task;
                sourceCtrl.RefreshPosition();
                destCtrl.RefreshPosition();
            }
        } // OnFileMoveRequest

        private async void SetTreeVisibility(bool visible)
        {
            AppSettings.ShowTree = visible;
            m_ShowTreeMenu.IsChecked = visible;

            if (!visible) {
                m_InnerGrid.ColumnDefinitions[0].MinWidth = 0;
                m_InnerGrid.ColumnDefinitions[0].Width = new GridLength(0);
                m_InnerGrid.ColumnDefinitions[1].Width = new GridLength(0);

                m_LeftTabControl.Margin = new Thickness(-5, 0, 0, 0);
            } else {
                m_InnerGrid.ColumnDefinitions[0].MinWidth = 100;
                m_InnerGrid.ColumnDefinitions[0].Width = new GridLength(2, GridUnitType.Star);
                m_InnerGrid.ColumnDefinitions[1].Width = GridLength.Auto;

                m_LeftTabControl.Margin = new Thickness(0, 0, 0, 0);
                await m_Tree.Set(m_ActiveFileManager, m_ActiveFileManager.Position);
            }
            m_Tree.IsVisible = visible;
            m_GridSplitter.IsVisible = visible;
        } // SetTreeVisibility

        private void SetShowHiddenFiles(bool show)
        {
            AppSettings.ShowHiddenFiles = show;
            m_ShowHiddenFilesMenu.IsChecked = show;

            var tabs = new List<TabControl>() { m_LeftTabControl, m_RightTabControl };
            foreach(var tab in tabs) {
                foreach (var i in tab.Items) {
                    var ti = i as TabItem;
                    var fm = ti.Content as FileManagerControl;
                    fm.ShowHiddenFiles = show;
                }
            }
        } // SetShowHiddenFiles
        public void ShowNotification(NotificationType type, string title, string message)
        {
            m_NotificationManager.Show(new Notification(title, message, type));
        } // ShowNotification

        private void OnWindowClosing(object sender, CancelEventArgs args)
        {
            var settings = AppSettings;
            settings.OpenedTabs = new List<Utils.Settings.OpenedTab>();
            if (settings.SaveOpenedTabsOnExit) {
                var items = m_LeftTabControl.ItemsSource as Avalonia.Collections.AvaloniaList<object>;
                foreach (var item in items) {
                    var ti = item as TabItem;
                    settings.OpenedTabs.Add(new Utils.Settings.OpenedTab()
                    {
                        TabPosition = Utils.Settings.OpenedTab.TabPositions.Left,
                        ViewMode = (ti.Content as FileManagerControl).ViewMode,
                        Position = (ti.Content as FileManagerControl).GetBookmark().GetPosition()
                    });
                }

                items = m_RightTabControl.ItemsSource as Avalonia.Collections.AvaloniaList<object>;
                foreach (var item in items) {
                    var ti = item as TabItem;
                    settings.OpenedTabs.Add(new Utils.Settings.OpenedTab()
                    {
                        TabPosition = Utils.Settings.OpenedTab.TabPositions.Right,
                        ViewMode = (ti.Content as FileManagerControl).ViewMode,
                        Position = (ti.Content as FileManagerControl).GetBookmark().GetPosition()
                    });
                }
            }

            settings.Save(App.SettingsPath);
        } // OnWindowClosing

        private void OnTabPointerPressed(object sender, PointerPressedEventArgs args)
        {
            var ti = ((StackPanel)sender).Parent as TabItem;
            if (args.GetCurrentPoint(this).Properties.IsMiddleButtonPressed) {
                m_ClickedTab = ti;
            } else {
                m_ClickedTab = null;
            }
        } // OnTabPointerPressed

        private void OnTabPointerReleased(object sender, PointerReleasedEventArgs args)
        {
            var ti = ((StackPanel)sender).Parent as TabItem;
            if (args.InitialPressMouseButton == MouseButton.Middle && ti == m_ClickedTab) {
                RemoveTab(ti);
            }
        } // OnTabPointerReleased

        private void SetToggleIcons()
        {
            if (m_ActiveFileManager.ViewMode == FileListingControl.ViewModes.List) {
                Projektanker.Icons.Avalonia.Attached.SetIcon(m_ToggleViewBtn, "fas fa-th-list");
                m_ViewListMenu.IsChecked = true;
            } else {
                Projektanker.Icons.Avalonia.Attached.SetIcon(m_ToggleViewBtn, "fas fa-th-large");
                m_ViewIconMenu.IsChecked = true;
            }
        }
        #region menu events

        private async void OnNewTabClicked(object sender, RoutedEventArgs args)
        {
            await AddNewTab(m_ActiveFileManager.Parent.Parent as TabControl, m_ActiveFileManager.Position);
        } // OnNewTabClicked

        private void OnCloseTabClicked(object sender, RoutedEventArgs args)
        {
            RemoveTab(Utils.Utils.FindParent<TabItem>(m_ActiveFileManager));
        } // OnCloseTabClicked

        private void OnCreateDirClicked(object sender, RoutedEventArgs args)
        {
            if (m_ActiveFileManager != null)
                m_ActiveFileManager.CreateDirectory();
        } // OnCreateDirClicked

        private void OnExitClicked(object sender, RoutedEventArgs args)
        {
            Close();
        } // OnExitClicked

        private void OnShowHiddenFilesChanged(object sender, RoutedEventArgs args)
        {
            SetShowHiddenFiles((sender as CheckableMenuItem).IsChecked);
        } // OnShowHiddenFilesChanged

        private void OnShowTreeChanged(object sender, RoutedEventArgs args)
        {
            SetTreeVisibility((sender as CheckableMenuItem).IsChecked);
        } // OnShowTreeChanged

        private void OnViewModeChanged(object sender, RoutedEventArgs args)
        {
            var mi = (CheckableMenuItem)sender;
            if (mi.IsChecked) {
                if ((string)mi.Tag == "List")
                    m_ActiveFileManager.ViewMode = FileListingControl.ViewModes.List;
                else
                    m_ActiveFileManager.ViewMode = FileListingControl.ViewModes.Icon;
                SetToggleIcons();
            }
        } // OnViewModeChanged

        #endregion

        #region toolbar
        private void OnToggleTreeViewClicked(object sender, RoutedEventArgs args)
        {
            SetTreeVisibility(!m_Tree.IsVisible);
        } // OnToggleTreeViewClicked

        private void OnToggleViewClicked(object sender, RoutedEventArgs args)
        {
            if (m_ActiveFileManager.ViewMode == FileListingControl.ViewModes.List) {
                m_ActiveFileManager.ViewMode = FileListingControl.ViewModes.Icon;
            } else {
                m_ActiveFileManager.ViewMode = FileListingControl.ViewModes.List;
            }
            SetToggleIcons();
        } // OnToggleViewClicked
        #endregion
    }
}
