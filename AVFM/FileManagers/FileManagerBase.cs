using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AVFM.FileManagers
{
    public class FileInfo {
        public string FullPath { get; set; }
        public string Name { get;set; }
        public string Extension { get; set; }
        public string Type { get; set; }
        public string Icon { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public FileAttributes Attributes {get; set; }

        public bool IsHidden => Attributes.HasFlag(FileAttributes.Hidden);

        public bool IsFakeDirectory => Name == "." || Name == "..";
    } // FileInfo

    public class PositionInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
    } // PositionInfo

    public class DriveInfo {
        public string Name { get; set; }
        public string VolumeLabel { get; set; }
        public string Format { get; set; }
        public long Size { get; set; }
        public long FreeSpace { get; set; }
    } // DriveInfo

    public abstract class FileManagerBase : IDisposable
    {
        public abstract class Bookmark
        {
            public string Name { get; set; }
            public string Position { get; set; }
            [JsonIgnore]
            public string Icon { get; set; }

            public abstract string GetPosition();
        } // Bookmark

        public const string WindowsDriveMimeType = "inode/drive";
        public const string DirectoryMimeType = "inode/directory";
        public const string UnkownMimeType = "application/unknown";

        public FileManagerBase()
        {
            IconProvider = MimeIconProviders.MimeIconProviderFactory.GetMimeIconProvider(MimeIconProviders.MimeIconProviderBase.IconSizes.Size32x32);
        }

        protected MimeIconProviders.MimeIconProviderBase IconProvider { get; set; }

        public abstract void Dispose();
        public abstract Bookmark GetBookmark();
        public abstract string GetRoot();
        public abstract Task<List<DriveInfo>> GetDrives();
        public abstract Task<DriveInfo> GetDriveInfo(string path);
        public abstract Task<PositionInfo> GetPositionInfo(string path);
        public abstract Task<FileInfo> GetFileInfo(string filePath);
        public abstract Task<List<FileInfo>> GetDirectoryList(string position, bool includeHidden = true);
        public abstract Task<bool> HasSubdirs(string position);
        public abstract Task<List<FileInfo>> GetFileList(string position, CancellationToken cancellationToken, bool includeHidden = true);
        public abstract bool OpenFileWithDefaultApplication(string filePath);
        public abstract Task<bool> DirectoryExists(string path);
        public abstract Task<bool> CreateDirectory(string path);
        public abstract Task<bool> FileExists(string path);
        public abstract Task<bool> Rename(string path, string newName);
        public abstract Task<bool> Delete(string path);
        public abstract Task<Stream> OpenFile(string filePath, bool write = false, long? size = null);
        public abstract Task<bool> CloseFile(Stream stream);
        public abstract string CombinePath(string path1, string path2, string path3 = null);

        public virtual string GetPathSeparator()
        {
            return "/";
        } // GetPathSeparator

        public virtual string GetFileMimeType(string filePath)
        {
            int idx = filePath.LastIndexOf(".");
            if (idx >= 0) {
                var ext = filePath.Substring(idx);
                return Utils.Utils.GetMimeTypeFromFileExtension(ext);
            }
            return null;
        } // GetFileMimeType

        public virtual async Task<string> GetMimeIcon(string mimeType, string path = null)
        {
            if (IconProvider != null)
                return await IconProvider.GetMimeIcon(mimeType, path);
            return string.Empty;
        } // GetMimeIcon

        public FileInfo GetUpFolder(DirectoryInfo currDir) {
            return new FileInfo()
            {
                Name = "..",
                FullPath = currDir.Parent.FullName,
                Type = DirectoryMimeType,
                IsDirectory = true,
            };
        } // GetUpFolder
    }

    public static class FileManagerFactory
    {
        public static FileManagerBase GetFileManager(string position, FsFileManager defaultFsFileManager, out string newPosition)
        {
            newPosition = position;
            // FTP
            // ftp://user:password@host/position
            if (position.StartsWith("ftp://")) {
                var m = Regex.Match(position, "ftp://(([^:]+):([^@]+)@)?([^/:]+)(:[0-9]+)?(.*)?$");
                if (m.Success) {
                    if (m.Groups.Count == 7) {
                        var ftpFm = new FtpFileManager();
                        ftpFm.Credentials = new System.Net.NetworkCredential(m.Groups[2].Value, m.Groups[3].Value);
                        ftpFm.Host = m.Groups[4].Value;
                        if (!string.IsNullOrEmpty(m.Groups[5].Value))
                            ftpFm.Port = int.Parse(m.Groups[5].Value.Remove(0, 1));
                        if (!string.IsNullOrEmpty(m.Groups[6].Value))
                            newPosition = m.Groups[6].Value;
                        else
                            newPosition = "/";
                        return ftpFm;
                    } else
                        throw new Exception("Invalid address");
                } else
                    throw new Exception("Invalid address");
            }

            // SFTP
            // sftp://user:password@host/position
            // sftp://user:keyfile=pathToFile@host/position
            if (position.StartsWith("sftp://")) {
                var m = Regex.Match(position, "sftp://(([^:]+):([^@]+)@)?([^/:]+)(:[0-9]+)?(.*)?$");
                if (m.Success) {
                    if (m.Groups.Count == 7) {
                        var sftpFm = new SftpFileManager();
                        sftpFm.Host = m.Groups[4].Value;
                        sftpFm.UserName = m.Groups[2].Value;
                        sftpFm.Password = m.Groups[3].Value;
                        if (sftpFm.Password.StartsWith("keyfile=")) {
                            sftpFm.PrivateKeyFile = sftpFm.Password.Substring(8);
                            sftpFm.Password = string.Empty;
                        }
                        if (!string.IsNullOrEmpty(m.Groups[5].Value))
                            sftpFm.Port = int.Parse(m.Groups[5].Value.Remove(0, 1));
                        if (!string.IsNullOrEmpty(m.Groups[6].Value))
                            newPosition = m.Groups[6].Value;
                        else
                            newPosition = "/";
                        return sftpFm;
                    } else
                        throw new Exception("Invalid address");
                } else
                    throw new Exception("Invalid address");
            }

            // SMB
            // smb://domain\user:password@host\position
            if (position.StartsWith("smb://")) {
                var m = Regex.Match(position, "smb://((.+)?\\\\(.+):(.+)@)?([^/]+)?(.*)?$");
                if (m.Success) {
                    if (m.Groups.Count == 7) {
                        var smbFm = new SmbFileManager(m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value, m.Groups[5].Value);
                        if (!string.IsNullOrEmpty(m.Groups[6].Value))
                            newPosition = $"smb://{smbFm.Host}{m.Groups[6].Value}";
                        else
                            newPosition = $"smb://{smbFm.Host}/";
                        return smbFm;
                    } else
                        throw new Exception("Invalid address");
                } else
                    throw new Exception("Invalid address");
            }

            // OneDrive
            // onedrive://accessToken:refreshToken:position
            if (position.StartsWith("onedrive://")) {
                var m = Regex.Match(position, "onedrive://([^:]+)?:([^:]+)?:(.*)$");
                if (m.Success) {
                    string accessToken = null;
                    string refreshToken = null;
                    if (m.Groups.Count == 4) {
                        accessToken = m.Groups[1].Value;
                        refreshToken = m.Groups[2].Value;
                    }
                    if (!string.IsNullOrEmpty(m.Groups[3].Value))
                        newPosition = $"onedrive://{m.Groups[3].Value}";
                    else
                        newPosition = "onedrive:///";
                    var odFm = new OneDriveFileManager(accessToken, refreshToken);
                    return odFm;
                } else
                    throw new Exception("Invalid address");
            }

            if (App.DefaultFsFileManager != null)
                return App.DefaultFsFileManager;

            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return new FsFileManagerLinux();

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                return new FsFileManagerWindows();

            return null;
        } // GetFileManager
    } // FileManagerFactory
}