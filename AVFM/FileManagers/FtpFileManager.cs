using System;
using System.Collections.Generic;
using System.IO;
using FluentFTP;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace AVFM.FileManagers
{
    public class FtpFileManager : FileManagerBase
    {
        public class FtpBookmark : Bookmark
        {
            public FtpBookmark()
            {
                Icon = "fas fa-network-wired";
                Port = 21;
                MaximumConnections = 2;
            }

            public string Host { get; set; }
            public int Port { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public int MaximumConnections { get; set; }

            public override string GetPosition()
            {
                return $"ftp://{Username}:{Password}@{Host}:{Port}{Position}";
            }
        } // FtpBookmark

        class Client : IDisposable
        {
            public Client(FtpClient client)
            {
                Ftp = client;
            }

            public bool IsBusy { get; set; }
            public FtpClient Ftp { get; set; }
            public Stream OpenedStream { get; set; }

            public void Dispose()
            {
                IsBusy = false;
            }
        }

        private List<Client> m_Clients = new List<Client>();
        private SemaphoreSlim m_ClientsSema = new SemaphoreSlim(1, 1);

        public FtpFileManager() :
            base()
        {
            Port = 21;
            SslProtocols = System.Security.Authentication.SslProtocols.None;
            MaximumConnections = 2;
        }

        public string Host { get; set; }
        public int Port { get; set; }
        public int MaximumConnections { get; set; }
        public System.Security.Authentication.SslProtocols SslProtocols { get; set; }
        public NetworkCredential Credentials { get; set; }

        public override void Dispose()
        {
            foreach (var c in m_Clients) {
                c.Ftp.Dispose();
                c.Dispose();
            }

            m_ClientsSema.Dispose();
        }

        public override async Task<bool> CloseFile(Stream stream)
        {
            await m_ClientsSema.WaitAsync();
            try {
                var client = m_Clients.FirstOrDefault(c => c.OpenedStream == stream);
                await Task.Run(() => client.Ftp.GetReply());
                client.OpenedStream = null;
                client.IsBusy = false;
                stream.Dispose();
            } catch {

            } finally {
                m_ClientsSema.Release();
            }

            return true;
        } // CloseFile

        public override string CombinePath(string path1, string path2, string path3 = null)
        {
            List<string> paths = new List<string>() { path1, path2, path3 };
            string res = string.Empty;
            foreach (var path in paths) {
                if (path != null) {
                    if (!string.IsNullOrEmpty(res) && !res.EndsWith(GetPathSeparator()) && !path.StartsWith(GetPathSeparator()))
                        res = $"{res}{GetPathSeparator()}{path}";
                    else
                        res = $"{res}{path}";
                }
            }
            return res;
        } // CombinePath

        public override async Task<bool> CreateDirectory(string path)
        {
            path = GetPath(path);
            using (var client = await GetClient()) {
                if (await DirectoryExists(path))
                    throw new Exception($"Cannot create '{path}' because a directory with the same name already exists");
                return await Task.Run(() => client.Ftp.CreateDirectory(path));
            }
        } // CreateDirectory

        public override async Task<bool> Delete(string path)
        {
            path = GetPath(path);
            using (var client = await GetClient()) {
                var fi = await Task.Run(() => client.Ftp.GetObjectInfo(path));
                if (fi.Type == FtpObjectType.Directory)
                    await Task.Run(() => client.Ftp.DeleteDirectory(path));
                else
                    await Task.Run(() => client.Ftp.DeleteFile(path));
                return true;
            }
        } // Delete

        public override async Task<bool> DirectoryExists(string path)
        {
            path = GetPath(path);
            using (var client = await GetClient()) {
                return await Task.Run(() => client.Ftp.DirectoryExists(path));
            }
        } // DirectoryExists

        public override async Task<bool> FileExists(string path)
        {
            path = GetPath(path);
            using (var client = await GetClient()) {
                return await Task.Run(() => client.Ftp.FileExists(path));
            }
        } // FileExists

        public override Bookmark GetBookmark()
        {
            return new FtpBookmark() {
                Host = Host,
                Port = Port,
                Username = Credentials?.UserName,
                Password = Credentials?.Password,
            };
        } // GetBookmark

        public override async Task<bool> HasSubdirs(string position)
        {
            position = GetPath(position);
            using (var client = await GetClient()) {
                using (CancellationTokenSource cts = new CancellationTokenSource()) {
                    foreach (var f in await Task.Run(() => client.Ftp.GetListing(position), cts.Token)) {
                        if (f.Name != "." && f.Name != ".." && f.Type == FtpObjectType.Directory) {
                            cts.Cancel();
                            return true;
                        }
                    }
                }
            }
            return false;
        } // HasSubdirs

        public override async Task<List<FileInfo>> GetDirectoryList(string position, bool includeHidden = true)
        {
            position = GetPath(position);
            List<FileInfo> res = new List<FileInfo>();
            using (var client = await GetClient()) {
                foreach (var f in await Task.Run(() => client.Ftp.GetListing(position))) {
                    if (f.Type == FtpObjectType.Directory) {
                        res.Add(new FileInfo()
                        {
                            FullPath = $"ftp://{Host}{f.FullName}",
                            Name = f.Name,
                            Type = DirectoryMimeType,
                            Size = f.Size,
                            LastModified = f.Modified,
                            Created = f.Created,
                            IsDirectory = true
                        });
                    }
                }
            }
            return res;
        } // GetDirectoryList

        public override async Task<List<DriveInfo>> GetDrives()
        {
            return new List<DriveInfo>()
            {
                await GetDriveInfo(GetRoot())
            };
        } // GetDrives

        public override Task<DriveInfo> GetDriveInfo(string path)
        {
            path = GetPath(path);
            return Task.FromResult(new DriveInfo() {
                Name = $"ftp://{Host}/",
                VolumeLabel = Host
            });
        } // GetDriveInfo

        public override async Task<FileInfo> GetFileInfo(string filePath)
        {
            filePath = GetPath(filePath);
            using (var client = await GetClient()) {
                var f = await Task.Run(() => client.Ftp.GetObjectInfo(filePath, true));
                return new FileInfo() {
                    FullPath = $"ftp://{Host}{f.FullName}",
                    Name = f.Name,
                    Size = f.Size,
                    LastModified = f.Modified,
                    Created = f.Created,
                    IsDirectory = f.Type == FtpObjectType.Directory
                };
            }
        } // GetFileInfo

        public override async Task<List<FileInfo>> GetFileList(string position, CancellationToken cancellationToken, bool includeHidden = true)
        {
            position = GetPath(position);
            List<FileInfo> res = new List<FileInfo>();

            if (position != "/") {
                int idx = position.LastIndexOf(GetPathSeparator());
                if (idx >= 0) {
                    res.Add(new FileInfo() {
                        Name = "..",
                        FullPath = $"ftp://{Host}{position.Substring(0, idx)}",
                        Type = DirectoryMimeType,
                        IsDirectory = true,
                    });
                }
            }

            using (var client = await GetClient()) {
                foreach (var f in await Task.Run(() => client.Ftp.GetListing(position))) {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    var fi = new FileInfo() {
                        FullPath = $"ftp://{Host}{f.FullName}",
                        Name = f.Name,
                        Size = f.Size,
                        LastModified = f.Modified,
                        Created = f.Created,
                        IsDirectory = f.Type == FtpObjectType.Directory
                    };

                    if (fi.IsDirectory)
                        fi.Type = DirectoryMimeType;
                    res.Add(fi);
                }
                return res;
            }
        } // GetFileList

        public override string GetRoot()
        {
            return $"ftp://{Host}/";
        } // GetRoot

        public override Task<PositionInfo> GetPositionInfo(string path)
        {
            path = GetPath(path);
            var name = path;
            int idx = path.LastIndexOf(GetPathSeparator());
            if (idx > 0 || name.Length > 1)
                name = path.Substring(idx + 1);
            if (string.IsNullOrEmpty(name))
                name = GetPath(GetRoot());

            return Task.FromResult(new PositionInfo()
            {
                Name = name,
                FullName = $"ftp://{Host}{path}"
            });
        } // GetPositionInfo

        public override async Task<Stream> OpenFile(string filePath, bool write = false, long? size = null)
        {
            filePath = GetPath(filePath);
            using (var client = await GetClient()) {
                Stream res = null;
                if (!write)
                    res = await Task.Run(() => client.Ftp.OpenRead(filePath));
                else
                    res = await Task.Run(() => client.Ftp.OpenWrite(filePath));

                client.OpenedStream = res;
                return res;
            }
        } // OpenFile

        public override bool OpenFileWithDefaultApplication(string filePath)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> Rename(string path, string newName)
        {
            path = GetPath(path);
            using (var client = await GetClient()) {
                await Task.Run(() => client.Ftp.Rename(path, newName));
                return true;
            }
        } // Rename

        private async Task<Client> GetClient()
        {
            Client res = null;
            while (res == null) {
                await m_ClientsSema.WaitAsync();
                if (m_Clients.Count < MaximumConnections) {
                    var ftpClient = new FtpClient(Host, Credentials, Port, new FtpConfig()
                    {
                        SocketKeepAlive = true,
                        EncryptionMode = FtpEncryptionMode.Auto,
                        SslProtocols = SslProtocols,
                        ValidateAnyCertificate = true,
                    });

                    res = new Client(ftpClient);
                    m_Clients.Add(res);
                } else {
                    res = m_Clients.FirstOrDefault(c => !c.IsBusy && c.OpenedStream == null);
                }

                if (res != null) {
                    if (!res.Ftp.IsConnected)
                        res.Ftp.Connect();
                    res.IsBusy = true;
                }
                m_ClientsSema.Release();
                if (res == null)
                    await Task.Delay(10);
            }

            return res;
        } // GetClient

        private string GetPath(string position)
        {
            if (position.StartsWith($"ftp://{Host}")) {
                return position.Remove(0, $"ftp://{Host}".Length);
            }
            return position;
        } // GetPath
    }
}