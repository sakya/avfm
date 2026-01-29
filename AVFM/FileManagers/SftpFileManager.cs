using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Renci.SshNet;

namespace AVFM.FileManagers
{
    public class SftpFileManager : FileManagerBase
    {
        public class SftpBookmark : Bookmark
        {
            public SftpBookmark()
            {
                Icon = "fas fa-network-wired";
                Port = 22;
                MaximumConnections = 2;
            }

            public string Host { get; set; }
            public int Port { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string PrivateKeyFile { get; set; }
            public int MaximumConnections { get; set; }

            public override string GetPosition()
            {
                if (!string.IsNullOrEmpty(PrivateKeyFile))
                    return $"sftp://{Username}:file={PrivateKeyFile}@{Host}:{Port}{Position}";
                return $"sftp://{Username}:{Password}@{Host}:{Port}{Position}";
            }
        } // SftpBookmark

        class Client : IDisposable
        {
            public Client(SftpClient client)
            {
                Sftp = client;
            }

            public bool IsBusy { get; set; }
            public SftpClient Sftp { get; set; }

            public void Dispose()
            {
                IsBusy = false;
            }
        }

        private List<Client> m_Clients = new List<Client>();
        private SemaphoreSlim m_ClientsSema = new SemaphoreSlim(1, 1);

        public SftpFileManager() :
            base()
        {
            Port = 22;
            MaximumConnections = 2;
        }

        public string Host { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string PrivateKeyFile { get; set; }
        public int Port { get; set; }
        public int MaximumConnections { get; set; }

        public override Task<bool> CloseFile(Stream stream)
        {
            stream.Dispose();
            return Task.FromResult(true);
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
                client.Sftp.CreateDirectory(path);
                return true;
            }
        } // CreateDirectory

        public override async Task<bool> Delete(string path)
        {
            path = GetPath(path);
            using (var client = await GetClient()) {
                var fi = client.Sftp.Get(path);
                if (fi.IsDirectory)
                    client.Sftp.DeleteDirectory(path);
                else
                    client.Sftp.Delete(path);
                return true;
            }
        } // Delete

        public override async Task<bool> DirectoryExists(string path)
        {
            path = GetPath(path);
            using (var client = await GetClient()) {
                var res = client.Sftp.Exists(path);
                if (res) {
                    var item = client.Sftp.Get(path);
                    return item.Attributes.IsDirectory;
                }
                return res;
            }
        } // DirectoryExists

        public override void Dispose()
        {
            foreach (var c in m_Clients) {
                c.Sftp.Dispose();
                c.Dispose();
            }

            m_ClientsSema.Dispose();
        } // Dispose

        public override async Task<bool> FileExists(string path)
        {
            path = GetPath(path);
            using (var client = await GetClient()) {
                var res = client.Sftp.Exists(path);
                if (res) {
                    var item = client.Sftp.Get(path);
                    return !item.Attributes.IsDirectory;
                }
                return res;
            }
        } // FileExists

        public override Bookmark GetBookmark()
        {
            return new SftpBookmark() {
                Host = Host,
                Port = Port,
                Username = UserName,
                Password = Password,
                PrivateKeyFile = PrivateKeyFile
            };
        } // GetBookmark

        public override async Task<List<FileInfo>> GetDirectoryList(string position, bool includeHidden = true)
        {
            return await Task.Run(async () => await GetDirectoryListPrimitive(position, includeHidden));
        } // GetDirectoryList

        private async Task<List<FileInfo>> GetDirectoryListPrimitive(string position, bool includeHidden = true)
        {
            position = GetPath(position);
            List<FileInfo> res = new List<FileInfo>();
            using (var client = await GetClient()) {
                foreach (var f in client.Sftp.ListDirectory(position)) {
                    if (f.IsDirectory && f.Name != ".." && f.Name != ".") {
                        var fi = new FileInfo() {
                            FullPath = $"sftp://{Host}{f.FullName}",
                            Name = f.Name,
                            Size = f.Length,
                            LastModified = f.Attributes.LastWriteTime,
                            Created = DateTime.MinValue,
                            IsDirectory = f.Attributes.IsDirectory,
                            Type = DirectoryMimeType
                        };
                        res.Add(fi);
                    }
                }
            }
            return res;
        } // GetDirectoryListPrimitive

        public override Task<DriveInfo> GetDriveInfo(string path)
        {
            path = GetPath(path);
            return Task.FromResult(new DriveInfo() {
                Name = $"sftp://{Host}/",       
                VolumeLabel = Host  
            });
        } // GetDriveInfo

        public override async Task<List<DriveInfo>> GetDrives()
        {
            return new List<DriveInfo>()
            {
                await GetDriveInfo(GetRoot())
            };
        } // GetDrives

        public override async Task<FileInfo> GetFileInfo(string filePath)
        {
            filePath = GetPath(filePath);
            using (var client = await GetClient()) {
                var f = client.Sftp.Get(filePath);
                return new FileInfo() {
                    FullPath = $"sftp://{Host}{f.FullName}",
                    Name = f.Name,
                    Size = f.Length,
                    LastModified = f.Attributes.LastWriteTime,
                    Created = DateTime.MinValue,
                    IsDirectory = f.IsDirectory
                };                
            }
        } // GetFileInfo

        public override async Task<List<FileInfo>> GetFileList(string position, CancellationToken cancellationToken, bool includeHidden = true)
        {
            return await Task.Run( async () => await GetFileListPrimitive(position, cancellationToken, includeHidden));
        } // GetFileList

        private async Task<List<FileInfo>> GetFileListPrimitive(string position, CancellationToken cancellationToken, bool includeHidden = true)
        {
            position = GetPath(position);
            List<FileInfo> res = new List<FileInfo>();
            using (var client = await GetClient()) {
                foreach (var f in client.Sftp.ListDirectory(position)) {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    if (f.Name == ".." || f.Name == ".")
                        continue;

                    var fi = new FileInfo() {
                        FullPath = $"sftp://{Host}{f.FullName}",
                        Name = f.Name,
                        Size = f.Length,
                        LastModified = f.Attributes.LastWriteTime,
                        Created = DateTime.MinValue,
                        IsDirectory = f.Attributes.IsDirectory
                    };

                    if (fi.IsDirectory)
                        fi.Type = DirectoryMimeType;
                    res.Add(fi);                    
                }
            }
            return res;
        } // GetFileListPrimitive

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
                FullName = $"sftp://{Host}{path}"
            });
        } // GetPositionInfo

        public override string GetRoot()
        {
            return $"sftp://{Host}/";
        } // GetRoot

        public override async Task<bool> HasSubdirs(string position)
        {
            position = GetPath(position);
            using (var client = await GetClient()) {
                foreach (var f in client.Sftp.ListDirectory(position)) {
                    if (f.Name != "." && f.Name != ".." && f.Attributes.IsDirectory) {
                        return true;
                    }
                }
            }
            return false;
        } // HasSubdirs

        public override async Task<Stream> OpenFile(string filePath, bool write = false, long? size = null)
        {
            filePath = GetPath(filePath);
            using (var client = await GetClient()) {
                if (write)
                    return client.Sftp.Open(filePath, FileMode.Create, FileAccess.Write);
                return client.Sftp.Open(filePath, FileMode.Open, FileAccess.Read);
            }
        } // OpenFile

        public override bool OpenFileWithDefaultApplication(string filePath)
        {
            throw new System.NotImplementedException();
        } // OpenFileWithDefaultApplication

        public override async Task<bool> Rename(string path, string newName)
        {
            path = GetPath(path);
            using (var client = await GetClient()) {
                int idx = path.LastIndexOf(GetPathSeparator());
                if (idx >= 0) {
                    var newPath = CombinePath(path.Substring(0, idx), newName);
                    client.Sftp.RenameFile(path, newPath);
                    return true;
                }
                return false;
            }
        } // Rename

        private async Task<Client> GetClient()
        {
            Client res = null;
            while (res == null) {
                await m_ClientsSema.WaitAsync();
                if (m_Clients.Count < MaximumConnections) {
                    SftpClient ftpClient = null;
                    if (!string.IsNullOrEmpty(PrivateKeyFile)) {
                        PrivateKeyFile[] pks = new PrivateKeyFile[] { new PrivateKeyFile(PrivateKeyFile) };
                        ftpClient = new SftpClient(Host, Port, UserName, pks);
                    } else {
                        ftpClient = new SftpClient(Host, Port, UserName, Password);
                    }
                    res = new Client(ftpClient);
                    m_Clients.Add(res);
                } else {
                    res = m_Clients.Where(c => !c.IsBusy).FirstOrDefault();
                }

                if (res != null) {
                    if (!res.Sftp.IsConnected)
                        res.Sftp.Connect();
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
            if (position.StartsWith($"sftp://{Host}")) {
                return position.Remove(0, $"sftp://{Host}".Length);
            }
            return position;
        } // GetPath           
    }
}