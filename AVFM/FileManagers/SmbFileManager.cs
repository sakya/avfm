using System;
using System.Collections.Generic;
using System.IO;
using EzSmb;
using System.Threading;
using System.Threading.Tasks;

namespace AVFM.FileManagers
{
    public class SmbFileManager : FileManagerBase
    {
        public class SmbBookmark : Bookmark
        {
            public SmbBookmark()
            {
                Icon = "fab fa-windows";
            }

            public string Host { get; set; }
            public string DomainName { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }

            public override string GetPosition()
            {
                return $"smb://{DomainName}\\{UserName}:{Password}@{Host}{Position}";
            }
        } // SmbBookmark

        public class WriterStream : Stream
        {
            private bool m_Abort = false;
            private string m_FileName = string.Empty;
            private Node m_Node = null;
            private Task m_WriteTask = null;
            private long m_Pos = 0;
            private long m_Length = 0;
            private long m_WrittenBytes = 0;

            private long m_CacheSize = 32768;
            private List<byte> m_Buffer = new List<byte>();
            private SemaphoreSlim m_BufferSema = new SemaphoreSlim(1,1);

            public WriterStream(Node node, string fileName, long? size)
            {
                m_Node = node;
                m_FileName = fileName;
                if (size.HasValue)
                    m_Length = size.Value;
            }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => m_Length;

            public override long Position { 
                get { return m_Pos; }
                set {
                    if (value != m_Pos)
                        m_Pos = value;                   
                }
            }

            public new void Dispose()
            {
                if (m_WriteTask != null) 
                    m_WriteTask.Dispose();

                if (m_Node != null)
                    m_Node.Dispose();
                base.Dispose();
            } // Dispose

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (m_Abort && m_WrittenBytes < m_Length || m_Pos >= m_Length)
                    return 0;

                m_BufferSema.Wait();
                int readCount = 0;
                for (int i = 0; i < count; i++) {
                    if (m_Buffer.Count > 0) {
                        buffer[offset + i] = m_Buffer[0];
                        m_Buffer.RemoveAt(0);

                        m_Pos++;
                        readCount++;
                        if (m_Pos >= m_Length)
                            break;                        
                    } else {
                        m_BufferSema.Release();
                        Thread.Sleep(10);
                        m_BufferSema.Wait();
                        i--;
                        if (m_Abort)
                            break;                        
                    }
                }
                m_BufferSema.Release();
                return readCount;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new InvalidOperationException();
            }

            public override void SetLength(long value)
            {
                m_Length = value;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (m_Abort)
                    return;

                if (m_WriteTask == null)
                    m_WriteTask = m_Node.Write(this, m_FileName);

                while(m_Buffer.Count >= m_CacheSize)
                    Thread.Sleep(10);

                m_BufferSema.Wait();
                for (int i = 0; i < count; i++) {
                    m_Buffer.Add(buffer[offset + i]);        
                }
                m_WrittenBytes += count;
                m_BufferSema.Release();
            }

            public async Task<bool> WaitTask()
            {
                if (m_WriteTask != null)
                        await m_WriteTask;
                return true;
            }

            public void Abort()
            {
                m_Abort = true;
            }
        } // WriterStream

        private string m_DomainName = null;        
        private string m_Username = null;
        private string m_Password = null;
        private string m_Host = null;

        public SmbFileManager(string domainName, string username, string password, string host) :
            base()
        {
            m_DomainName = domainName;
            m_Username = username;
            m_Password = password;
            m_Host = host;
        }

        public string Host {
            get { return m_Host; }
        }

        public override async Task<bool> CloseFile(Stream stream)
        {
            if (stream is WriterStream ws) {
                ws.Abort();
                await ws.WaitTask();
            }
            stream.Dispose();
            return true;
        } // CloseFile

        public override string CombinePath(string path1, string path2, string path3 = null)
        {
            List<string> paths = new List<string>() { GetPath(path1), path2, path3 };
            string res = string.Empty;
            foreach (var path in paths) {
                if (path != null) {
                    if (!string.IsNullOrEmpty(res) && !res.EndsWith(GetPathSeparator()) && !path.StartsWith(GetPathSeparator()))
                        res = $"{res}{GetPathSeparator()}{path}";
                    else 
                        res = $"{res}{path}";
                }
            }
            return $"smb://{res}";
        } // CombinePath

        public override async Task<bool> CreateDirectory(string path)
        {
            using (var node = await Node.GetNode(GetPath(path), GetParamSet())) {
                if (node != null && node.Type == NodeType.Folder)
                    throw new Exception($"Cannot create '{path}' because a directory with the same name already exists");
                // TODO
                //await node.CreateFolder()
            }
            return true;
        } // CreateDirectory

        public override async Task<bool> Delete(string path)
        {
            using (var node = await Node.GetNode(GetPath(path), GetParamSet())) {
                return await node.Delete();
            }
        } // Delete

        public override async Task<bool> DirectoryExists(string path)
        {
            using (var node = await Node.GetNode(GetPath(path), GetParamSet())) {
                return node != null && node.Type == NodeType.Folder;
            }
        } // DirectoryExists

        public override void Dispose()
        {
            
        }

        public override async Task<bool> FileExists(string path)
        {
            using (var node = await Node.GetNode(GetPath(path), GetParamSet())) {
                return node != null && node.Type == NodeType.File;
            }
        } // FileExists

        public override Bookmark GetBookmark()
        {
            return new SmbBookmark()
            {
                Host = m_Host,
                DomainName = m_DomainName,
                UserName = m_Username,
                Password = m_Password
            };
        } // GetBookmark

        public override async Task<bool> HasSubdirs(string position)
        {
            using (var folder = await Node.GetNode(GetPath(position), GetParamSet(), true)) {
                foreach (var node in await folder.GetList()) {
                    if (node.Type == NodeType.Folder)
                        return true;
                }
                return false;
            }
        } // HasSubdirs

        public override async Task<List<FileInfo>> GetDirectoryList(string position, bool includeHidden = true)
        {
            var res = new List<FileInfo>();

            using (var folder = await Node.GetNode(GetPath(position), GetParamSet(), true)) {
                foreach (var node in await folder.GetList()) {
                    if (node.Name == ".." || node.Name == "." || node.Type != NodeType.Folder)
                        continue;
                    res.Add(new FileInfo()
                    {
                        Name = node.Name,
                        FullPath = $"smb://{node.FullPath.Replace("\\", "/")}",
                        Size = node.Size.HasValue ? node.Size.Value : 0,
                        Created = node.Created.HasValue ? node.Created.Value : DateTime.MinValue,
                        LastModified = node.Updated.HasValue ? node.Updated.Value : DateTime.MinValue,
                        IsDirectory = node.Type == NodeType.Folder,
                        Type = node.Type == NodeType.Folder ? DirectoryMimeType : null,
                    });
                }
                return res;
            }
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
            return Task.FromResult(new DriveInfo()
            {
                Name = GetPath(GetRoot()),
                VolumeLabel = m_Host,
            });
        }

        public override async Task<FileInfo> GetFileInfo(string filePath)
        {
            using (var node = await Node.GetNode(GetPath(filePath), GetParamSet(), true)) {
                return new FileInfo()
                {
                    Name = node.Name,
                    FullPath = $"smb://{node.FullPath.Replace("\\", "/")}",
                    Size = node.Size.HasValue ? node.Size.Value : 0,
                    Created = node.Created.HasValue ? node.Created.Value : DateTime.MinValue,
                    LastModified = node.Updated.HasValue ? node.Updated.Value : DateTime.MinValue,
                    IsDirectory = node.Type == NodeType.Folder,
                    Type = node.Type == NodeType.Folder ? DirectoryMimeType : null,
                };
            }
        } // GetFileInfo

        public override async Task<List<FileInfo>> GetFileList(string position, CancellationToken cancellationToken, bool includeHidden = true)
        {
            var res = new List<FileInfo>();

            var folder = await Node.GetNode(GetPath(position), GetParamSet(), true);
            var parent = folder.GetParent();
            if (parent != null) {
                res.Add(new FileInfo()
                {
                    Name = "..",
                    FullPath = $"smb://{parent.FullPath.Replace("\\", "/")}",
                    Type = DirectoryMimeType,
                    IsDirectory = true,
                });            
            }

            foreach (var node in await folder.GetList()) {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (node.Name == ".." || node.Name == ".")
                    continue;
                res.Add(new FileInfo()
                {
                    Name = node.Name,
                    FullPath = $"smb://{node.FullPath.Replace("\\", "/")}",
                    Size = node.Size.HasValue ? node.Size.Value : 0,
                    Created = node.Created.HasValue ? node.Created.Value : DateTime.MinValue,
                    LastModified = node.Updated.HasValue ? node.Updated.Value : DateTime.MinValue,
                    IsDirectory = node.Type == NodeType.Folder,
                    Type = node.Type == NodeType.Folder ? DirectoryMimeType : null,
                });
            }
            return res;
        } // GetFileList

        public override string GetRoot()
        {
            return $"smb://{m_Host}/";
        } // GetRoot

        public override string GetPathSeparator()
        {
            return "/";
        } // GetPathSeparator

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
                FullName = $"smb://{path.Replace("\\", "/")}"
            });
        } // GetPositionInfo

        public override async Task<Stream> OpenFile(string filePath, bool write = false, long? size = null)
        {
            if (write) {
                int idx = filePath.LastIndexOf("/");
                if (idx >= 0) {
                    var folder = filePath.Substring(0, idx);
                    var fileName = filePath.Substring(idx + 1);
                    var node = await Node.GetNode(GetPath(folder), GetParamSet());
                    return new WriterStream(node, fileName, size);                  
                } else
                    throw new Exception($"Invalid path: {filePath}");
            } else {
                var node = await Node.GetNode(GetPath(filePath), GetParamSet());
                return node.GetReaderStream();
            }
        } // OpenFile

        public override bool OpenFileWithDefaultApplication(string filePath)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> Rename(string path, string newName)
        {
            using (var node = await Node.GetNode(GetPath(path), GetParamSet())) {
                int idx = path.LastIndexOf("/");
                if (idx >= 0) {
                    var folder = path.Substring(0, idx);
                    return await node.Move($"{m_Host}{folder}\\{newName}") != null;
                }
                return false;
            }
        } // Rename

        private EzSmb.Params.ParamSet GetParamSet()
        {
            return new EzSmb.Params.ParamSet()
            {
                DomainName = m_DomainName,
                UserName = m_Username,
                Password = m_Password,
                SmbType = EzSmb.Params.Enums.SmbType.Smb2
            };
        } // GetParamSet

        private string GetPath(string position)
        {
            if (position.StartsWith("smb://"))
                return position.Remove(0, 6);
            return position;
        } // GetPath
    }
}