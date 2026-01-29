using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AVFM.FileManagers
{
    public abstract class FsFileManager : FileManagerBase
    {
        public class FsBookmark : Bookmark
        {
            public FsBookmark()
            {
                Icon = "fas fa-hdd";
            }

            public override string GetPosition()
            {
                return Position;
            }
        } // FsBookmark

        public FsFileManager() :
            base()
        {

        }

        public override void Dispose()
        {

        }

        public override string GetRoot()
        {
            return "/";
        } // GetRoot


        public override Bookmark GetBookmark()
        {
            return new FsBookmark();
        } // GetBookmark

        public override Task<List<DriveInfo>> GetDrives()
        {
            List<DriveInfo> res = new List<DriveInfo>();
            foreach (var drive in System.IO.DriveInfo.GetDrives()) {
                var di = new DriveInfo()
                {
                    Name = drive.Name
                };

                try {
                    // These might fail (permissions, network drive not ready...)
                    di.VolumeLabel = drive.VolumeLabel;
                    di.Size = drive.TotalSize;
                    di.FreeSpace = drive.AvailableFreeSpace;
                    di.Format = drive.DriveFormat;
                } catch {

                }
                res.Add(di);
            }
            return Task.FromResult(res);
        } // GetDrives

        public override Task<DriveInfo> GetDriveInfo(string path)
        {
            var di = new DirectoryInfo(path);
            var drives = System.IO.DriveInfo.GetDrives()
                .OrderByDescending(d => d.RootDirectory.FullName.Length);
            foreach (var drive in drives) {
                if (di.FullName.StartsWith(drive.RootDirectory.FullName,
                    Environment.OSVersion.Platform == PlatformID.Win32NT ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture)) {
                    var res = new DriveInfo()
                    {
                        Name = drive.Name,
                    };

                    try {
                        // These might fail (permissions, network drive not ready...)
                        res.VolumeLabel = drive.VolumeLabel;
                        res.Size = drive.TotalSize;
                        res.FreeSpace = drive.AvailableFreeSpace;
                        res.Format = drive.DriveFormat;
                    } catch {

                    }
                    return Task.FromResult(res);
                }
            }
            return null;
        } // GetDriveInfo

        public override async Task<PositionInfo> GetPositionInfo(string path)
        {
            return await Task.Run( () => {
                var di = new DirectoryInfo(path);
                return new PositionInfo()
                {
                    Name = di.Name,
                    FullName = di.FullName
                };
            });
        } // GetPositionInfo

        public override Task<FileInfo> GetFileInfo(string filePath)
        {
            var fi = new System.IO.FileInfo(filePath);
            return Task.FromResult(new FileInfo() {
                FullPath = fi.FullName,
                Name = fi.Name,
                Extension = fi.Extension,
                Size = fi.Length,
                Attributes = fi.Attributes,
                Created = fi.CreationTime,
                LastModified = fi.LastWriteTime,
            });
        } // GetFileInfo

        public override async Task<bool> HasSubdirs(string position)
        {
            return await Task.Run( () => Directory.GetDirectories(position).Length > 0);
        } // HasSubdirs

        public override async Task<List<FileInfo>> GetDirectoryList(string position, bool includeHidden = true)
        {
            return await Task.Run(async () => await GetDirectoryListPrimitive(position, includeHidden));
        } // GetDirectoryList

        private Task<List<FileInfo>> GetDirectoryListPrimitive(string position, bool includeHidden = true)
        {
            var res = new List<FileInfo>();
            var di = new DirectoryInfo(position);

            foreach (var sdi in di.GetDirectories()) {
                if (sdi.Name == ".." || sdi.Name == ".")
                    continue;

                var fInfo = new FileInfo()
                {
                    FullPath = sdi.FullName,
                    Name = sdi.Name,
                    Type = DirectoryMimeType,
                    IsDirectory = true,
                    Attributes = sdi.Attributes,
                    Created = di.CreationTime,
                    LastModified = di.LastWriteTime,
                };

                if (Environment.OSVersion.Platform == PlatformID.Unix && fInfo.Name.StartsWith("."))
                    fInfo.Attributes = FileAttributes.Hidden;
                if (includeHidden || !fInfo.IsHidden)
                    res.Add(fInfo);
            }
            return Task.FromResult(res);
        } // GetDirectoryList

        public override async Task<List<FileInfo>> GetFileList(string position, CancellationToken cancellationToken, bool includeHidden = true)
        {
            return await Task.Run(async () => await GetFileListPrimitive(position, cancellationToken, includeHidden));
        } // GetFileList

        private Task<List<FileInfo>> GetFileListPrimitive(string position, CancellationToken cancellationToken, bool includeHidden = true)
        {
            List<FileInfo> res = new List<FileInfo>();
            DirectoryInfo di = new DirectoryInfo(position);

            if (di.Parent != null)
                res.Add(GetUpFolder(di));

            foreach (var sdi in di.GetDirectories()) {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (sdi.Name == ".." || sdi.Name == ".")
                    continue;

                var fInfo = new FileInfo()
                {
                    FullPath = sdi.FullName,
                    Name = sdi.Name,
                    Type = DirectoryMimeType,
                    IsDirectory = true,
                    Attributes = sdi.Attributes,
                    Created = di.CreationTime,
                    LastModified = di.LastWriteTime,
                };

                if (Environment.OSVersion.Platform == PlatformID.Unix && fInfo.Name.StartsWith("."))
                    fInfo.Attributes = FileAttributes.Hidden;
                if (includeHidden || !fInfo.IsHidden)
                    res.Add(fInfo);
            }

            foreach (var fi in di.GetFiles()) {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var fInfo = new FileInfo()
                {
                    FullPath = fi.FullName,
                    Name = fi.Name,
                    Extension = fi.Extension,
                    Size = fi.Length,
                    Attributes = fi.Attributes,
                    Created = fi.CreationTime,
                    LastModified = fi.LastWriteTime,
                };
                if (includeHidden || !fInfo.IsHidden)
                    res.Add(fInfo);
            }
            return Task.FromResult(res);
        } // GetFileListPrimitive

        public override Task<bool> DirectoryExists(string path)
        {
            return Task.FromResult(Directory.Exists(path));
        } // DirectoryExists

        public override async Task<bool> CreateDirectory(string path)
        {
            if (await DirectoryExists(path))
                throw new Exception($"Cannot create '{path}' because a directory with the same name already exists");
            Directory.CreateDirectory(path);
            return true;
        } // CreateDirectory

        public override Task<bool> FileExists(string path)
        {
            return Task.FromResult(File.Exists(path));
        } // FileExists

        public override Task<bool> Rename(string path, string newName)
        {
            if (Directory.Exists(path))
                Directory.Move(path, newName);
            else
                File.Move(path, newName);
            return Task.FromResult(true);
        } // Rename

        public override Task<bool> Delete(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path);
            else
                File.Delete(path);

            return Task.FromResult(true);
        } // Delete

        public override Task<Stream> OpenFile(string filePath, bool write = false, long? size = null)
        {
            Stream res = null;
            if (!write)
                res = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            else
                res = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            return Task.FromResult(res);
        } // OpenFile

        public override Task<bool> CloseFile(Stream stream)
        {
            stream.Dispose();
            return Task.FromResult(true);
        } // CloseFile

        public override string CombinePath(string path1, string path2, string path3 = null)
        {
            if (path3 == null)
                return Path.Combine(path1, path2);
            return Path.Combine(path1, path2, path3);
        } // CombinePath
    } // FsFileManager

    public class FsFileManagerLinux : FsFileManager
    {
        public FsFileManagerLinux() :
            base()
        {

        }

        public override string GetFileMimeType(string filePath)
        {
            string res = Utils.Utils.GetProcessOutput("xdg-mime", $"query filetype \"{filePath}\"");
            if (string.IsNullOrEmpty(res))
                return UnkownMimeType;
            return res;
        } // GetFileMimeType

        public override bool OpenFileWithDefaultApplication(string filePath)
        {
            ProcessStartInfo si = new ProcessStartInfo();
            si.FileName = "xdg-open";
            si.Arguments = $"\"{filePath}\"";
            si.WindowStyle = ProcessWindowStyle.Hidden;
            si.UseShellExecute = false;
            si.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = si;

            bool res = true;
            try {
                process.Start();
                process.WaitForExit();
                res = process.ExitCode == 0;
            } catch (Exception) {
                res = false;
            } finally {
                process.Dispose();
            }
            return res;
        } // OpenFileWithDefaultApplication
    } // FsFileManagerLinux

    public class FsFileManagerWindows : FsFileManager
    {
        public FsFileManagerWindows() :
            base()
        {

        }

        public override string GetFileMimeType(string filePath)
        {
            string mimeType = null;
            if (OperatingSystem.IsWindows()) {
                string ext = (filePath.Contains(".")) ? Path.GetExtension(filePath).ToLower() : "." + filePath;
                Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
                if (regKey != null && regKey.GetValue("Content Type") != null) mimeType = regKey.GetValue("Content Type").ToString();
            }
            if (mimeType == null)
                mimeType = base.GetFileMimeType(filePath);
            return string.IsNullOrEmpty(mimeType) ? UnkownMimeType : mimeType;
        } // GetFileMimeType

        public override string GetRoot()
        {
            return "\\";
        } // GetRoot

        public override bool OpenFileWithDefaultApplication(string filePath)
        {
            ProcessStartInfo si = new ProcessStartInfo();
            si.FileName = "cmd.exe";
            si.Arguments = $"/c \"{filePath}\"";
            si.WindowStyle = ProcessWindowStyle.Hidden;
            si.UseShellExecute = false;
            si.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = si;

            bool res = true;
            try {
                process.Start();
                process.WaitForExit();
                res = process.ExitCode == 0;
            } catch (Exception) {
                res = false;
            } finally {
                process.Dispose();
            }
            return res;
        } // OpenFileWithDefaultApplication

        public override string GetPathSeparator()
        {
            return "\\";
        } // GetPathSeparator
    } // FsFileManagerWindows
}