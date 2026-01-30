using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Newtonsoft.Json;
using System.Text;

namespace AVFM.Utils
{
    public abstract class ContextBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetIfChanged<T>(ref T target, T value, [CallerMemberName] string propertyName = "")
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            if (!EqualityComparer<T>.Default.Equals(target, value))
            {
                target = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    } // ContextBase

    public static class Utils
    {
        private static Dictionary<string, string> m_MimeTypes = null;
        public static string FormatFileName(FileManagers.FileInfo fi)
        {
            if (fi.IsDirectory && ((App)App.Current).Settings.ShowDirectoriesBetweenBrackets)
                return $"[{fi.Name}]";
            return fi.Name;
        } // FormatFileName

        public static string FormatSize(long size)
        {
            if (size < 0)
                size = 0;

            if (size / (1024.0 * 1024.0 * 1024.0) > 0.5)
                return Math.Round(size / (1024.0 * 1024.0 * 1024.0), 1).ToString("0.# Gib");
            if (size / (1024.0 * 1024.0) > 0.5)
                return Math.Round(size / (1024.0 * 1024.0), 1).ToString("0.# Mib");
            if (size / 1024.0 > 0.5)
                return Math.Round(size / 1024.0, 1).ToString("0.# Kib");
            return size.ToString("0 bytes");
        } // FormatSize


        public static string FormatDateTime(DateTime dt)
        {
            if (dt == DateTime.MinValue)
                return string.Empty;

            var customFormat = ((App)App.Current).Settings.DateTimeFormat;
            if (!string.IsNullOrEmpty(customFormat))
                return dt.ToString(customFormat);
            return dt.ToString(((App)App.Current).Settings.Culture);
        } // FormatDateTime


        public static string CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        } // CreateMD5

        public static string GetMimeTypeFromFileExtension(string ext, string defaultMime = "application/octet-stream")
        {
            if (m_MimeTypes == null) {
                Uri uri = new Uri($"avares://AVFM/Assets/mimeTypes.json");

                using (StreamReader sr = new StreamReader(AssetLoader.Open(uri), Encoding.UTF8)) {
                    m_MimeTypes = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());
                }
            }

            string res;
            if (m_MimeTypes != null && m_MimeTypes.TryGetValue(ext.ToLower(), out res))
                return res;
            return defaultMime;
        } // GetMimeTypeFromFileExtension

        public static string GetProcessOutput(string command, string arguments)
        {
            ProcessStartInfo si = new ProcessStartInfo();
            si.FileName = command;
            si.Arguments = arguments;
            si.WindowStyle = ProcessWindowStyle.Hidden;
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.RedirectStandardOutput = true;

            Process process = new Process();
            process.StartInfo = si;

            try {
                process.Start();
                var res = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return res;
            } catch (Exception) {
                return string.Empty;
            } finally {
                process.Dispose();
                process = null;
            }
        } // GetProcessOutput

        public static T FindParent<T>(Control control) where T: Control
        {
            var p = control?.Parent;
            while (p != null) {
                if (p is T)
                    return (T)p;
                p = p.Parent;
            }
            return null;
        } // FindParent
    }
}