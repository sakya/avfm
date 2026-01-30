using Newtonsoft.Json;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System;

namespace AVFM.Utils
{
    public class Settings
    {
        public enum FilterTypes
        {
            StartsWith,
            Contains,
            Regex
        }

        #region classes
        public class OpenedTab
        {
            public enum TabPositions
            {
                Left,
                Right,
            }

            public TabPositions TabPosition { get; set; }
            public Controls.FileListingControl.ViewModes ViewMode { get; set; }
            public string Position { get; set; }
        } // OpenedTab

        public class Shortcut
        {
            public enum Shortcuts
            {
                NewTab,
                CloseTab,

                GoBack,
                GoForward,
                GoHome,
                GoUp,

                Refresh,
                SelectFile,
                SelectAllFiles,

                MakeDir,
                ViewFile,
                RenameFile,
                CopyFile,
                MoveFile,
                DeleteFile,
            }
            public Shortcuts Type { get; set; }
            public Avalonia.Input.Key Key { get; set; }
            public Avalonia.Input.KeyModifiers KeyModifiers { get; set; }

            [JsonIgnore]
            public Avalonia.Input.KeyGesture InputGesture => new(Key, KeyModifiers);
        } // Shortcut
        #endregion

        public Settings() {
            Language = "en-US";
            HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            SortFolderBeforeFiles = true;
            ShowHiddenFiles = true;

            ConfirmCopy = true;
            ConfirmDelete = true;
            ConfirmMove = true;

            Bookmarks = [];
            Shortcuts = new Dictionary<Shortcut.Shortcuts, Shortcut>();
            // Default shortcuts
            SetShortcut(Shortcut.Shortcuts.NewTab, Avalonia.Input.Key.T, Avalonia.Input.KeyModifiers.Control);
            SetShortcut(Shortcut.Shortcuts.CloseTab, Avalonia.Input.Key.W, Avalonia.Input.KeyModifiers.Control);

            SetShortcut(Shortcut.Shortcuts.GoBack, Avalonia.Input.Key.Left, Avalonia.Input.KeyModifiers.Control);
            SetShortcut(Shortcut.Shortcuts.GoForward, Avalonia.Input.Key.Right, Avalonia.Input.KeyModifiers.Control);
            SetShortcut(Shortcut.Shortcuts.GoUp, Avalonia.Input.Key.Up, Avalonia.Input.KeyModifiers.Control);
            SetShortcut(Shortcut.Shortcuts.GoHome, Avalonia.Input.Key.H, Avalonia.Input.KeyModifiers.Control);

            SetShortcut(Shortcut.Shortcuts.Refresh, Avalonia.Input.Key.R, Avalonia.Input.KeyModifiers.Control);
            SetShortcut(Shortcut.Shortcuts.SelectFile, Avalonia.Input.Key.Space, Avalonia.Input.KeyModifiers.None);
            SetShortcut(Shortcut.Shortcuts.SelectAllFiles, Avalonia.Input.Key.A, Avalonia.Input.KeyModifiers.Control);

            SetShortcut(Shortcut.Shortcuts.MakeDir, Avalonia.Input.Key.F7, Avalonia.Input.KeyModifiers.None);
            SetShortcut(Shortcut.Shortcuts.ViewFile, Avalonia.Input.Key.F3, Avalonia.Input.KeyModifiers.None);
            SetShortcut(Shortcut.Shortcuts.RenameFile, Avalonia.Input.Key.F6, Avalonia.Input.KeyModifiers.Shift);
            SetShortcut(Shortcut.Shortcuts.CopyFile, Avalonia.Input.Key.F5, Avalonia.Input.KeyModifiers.None);
            SetShortcut(Shortcut.Shortcuts.MoveFile, Avalonia.Input.Key.F6, Avalonia.Input.KeyModifiers.None);
            SetShortcut(Shortcut.Shortcuts.DeleteFile, Avalonia.Input.Key.Delete, Avalonia.Input.KeyModifiers.None);
        }

        public string Language { get; set; }

        [JsonIgnore]
        public CultureInfo Culture
        {
            get
            {
                if (!string.IsNullOrEmpty(Language))
                    return CultureInfo.GetCultureInfo(Language);
                return CultureInfo.InvariantCulture;
            }
        }

        public Version Version { get; set; }
        public string HomePath { get; set; }

        public bool ShowDirectoriesBetweenBrackets { get; set; }
        public bool ShowHiddenFiles { get; set; }
        public bool ShowTree { get; set; }
        public bool SortFolderBeforeFiles { get; set; }
        public bool SelectFileWithRightClick { get; set; }
        public string DateTimeFormat { get; set; }
        public FilterTypes FilterType { get; set; }
        public bool ConfirmDelete { get; set; }
        public bool ConfirmCopy { get; set; }
        public bool ConfirmMove { get; set; }
        public bool SaveOpenedTabsOnExit { get; set; }

        public Dictionary<Shortcut.Shortcuts, Shortcut> Shortcuts { get; set; }
        public List<FileManagers.FileManagerBase.Bookmark> Bookmarks { get; set; }
        public List<OpenedTab> OpenedTabs { get; set; }

        public void SetShortcut(Shortcut.Shortcuts type, Avalonia.Input.Key key, Avalonia.Input.KeyModifiers modifier)
        {
            Shortcuts[type] = new Shortcut() {
                Type = type,
                Key = key,
                KeyModifiers = modifier
            };
        } // SetShortcut

        public Shortcut GetShortcut(Avalonia.Input.KeyEventArgs args)
        {
            foreach (var kvp in Shortcuts) {
                if (args.KeyModifiers == kvp.Value.KeyModifiers && args.Key == kvp.Value.Key) {
                    return kvp.Value;
                }
            }
            return null;
        } // GetShortcut

        public Shortcut GetShortcut(Shortcut.Shortcuts type)
        {
            return Shortcuts.GetValueOrDefault(type);
        } // GetShortcut

        public string GetHomePath()
        {
            if (!string.IsNullOrEmpty(HomePath))
                return HomePath;
            return  Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public static Settings Load(string path)
        {
            using var sr = new StreamReader(path);
            var settings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects };
            return JsonConvert.DeserializeObject<Settings>(sr.ReadToEnd(), settings);
        } // Load

        public void Save(string path)
        {
            using var sw = new StreamWriter(path);
            Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            var settings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects };
            sw.Write(JsonConvert.SerializeObject(this, Formatting.Indented, settings));
        } // Save
    }
}