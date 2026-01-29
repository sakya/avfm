using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace AVFM.MimeIconProviders
{
    public class WindowsMimeIconProvider : MimeIconProviderBase
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHSTOCKICONINFO
        {
            public uint cbSize;
            public IntPtr hIcon;
            public int iSysIconIndex;
            public int iIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szPath;
        }

        [DllImport("shell32.dll")]
        public static extern int SHGetStockIconInfo(uint siid, uint uFlags, ref SHSTOCKICONINFO psii);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr handle);

        private const uint SHSIID_FOLDER = 0x3;
        private const uint SHSIID_DRIVEREMOVE = 0x7;
        private const uint SHSIID_DRIVEFIXED = 0x8;
        private const uint SHSIID_DRIVENET = 0x9;

        private const uint SHGSI_ICON = 0x100;
        private const uint SHGSI_LARGEICON = 0x0;
        private const uint SHGSI_SMALLICON = 0x1;


        public WindowsMimeIconProvider(IconSizes size) :
            base(size)
        {

        }

        public override async Task<string> GetMimeIcon(string mimeType, string filePath)
        {
            return await Task.Run(() => GetMimeIconPrimitive(mimeType, filePath));
        } // GetMimeIcon

        private string GetMimeIconPrimitive(string mimeType, string filePath)
        {
            try {
                var iconFolder = Path.Combine(Path.GetTempPath(), "avfm_mimetypes");
                string fileName = null;
                var ext = Path.GetExtension(filePath).ToLower();
                if (ext == ".exe" || ext == ".ico")
                    fileName = Path.Combine(iconFolder, $"{Utils.Utils.CreateMD5(filePath)}.png");
                else
                    fileName = Path.Combine(iconFolder, $"{mimeType.Replace("/", "_")}.png");
                if (File.Exists(fileName))
                    return fileName;

                Icon icon = null;
                if (mimeType == FileManagers.FileManagerBase.DirectoryMimeType) {
                    icon = GetStockIcon(SHSIID_FOLDER, SHGSI_ICON);
                } else if (mimeType == FileManagers.FileManagerBase.WindowsDriveMimeType) {
                    icon = GetStockIcon(SHSIID_DRIVEFIXED, SHGSI_ICON);
                } else {
                    icon = Icon.ExtractAssociatedIcon(filePath);
                }

                if (icon != null) {
                    if (!Directory.Exists(iconFolder))
                        Directory.CreateDirectory(iconFolder);

                    icon.ToBitmap().Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                    return fileName;
                }
            } catch {

            }
            return null;
        } // GetMimeIconPrimitive

        private static Icon GetStockIcon(uint type, uint size)
        {
            var info = new SHSTOCKICONINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);

            SHGetStockIconInfo(type, SHGSI_ICON | size, ref info);

            var icon = (Icon)Icon.FromHandle(info.hIcon).Clone(); 
            DestroyIcon(info.hIcon); 
            return icon;
        } // GetStockIcon
    } // WindowsMimeIconProvider
}