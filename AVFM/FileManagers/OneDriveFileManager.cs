using System.Collections.Generic;
using System.IO;
using System;
using OneDriveRest;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AVFM.FileManagers
{
    public class OneDriveFileManager : FileManagerBase
    {
        public class OneDriveBookmark : Bookmark
        {
            public OneDriveBookmark()
            {
                Icon = "fas fa-cloud";
            }

            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }

            public override string GetPosition()
            {
                return $"onedrive://{AccessToken}:{RefreshToken}:{Position}";
            }
        } // OneDriveBookmark  

        public class OneDriveStream : Stream
        {   
            public enum Modes {
                Read,
                Write
            }

            private long m_Pos = 0;
            private long m_Size = 0;
            private OneDriveAPI m_Api = null;
            private string m_Url = null;
            private Modes m_Mode;

            private HttpWebResponse m_WebResponse = null;
            private Stream m_DownloadStream = null;

            public OneDriveStream(OneDriveAPI api, string url, Modes mode, long? size = null)
            {
                m_Api = api;
                m_Url = url;
                m_Mode = mode;
                m_Size = size ?? 0;              
            }

            public override bool CanRead => m_Mode == Modes.Read;

            public override bool CanSeek => false;

            public override bool CanWrite => m_Mode == Modes.Write;

            public override long Length => 0;

            public override long Position { get => m_Pos; set => throw new NotImplementedException(); }

            public new void Dispose()
            {
                if (m_DownloadStream != null)
                    m_DownloadStream.Dispose();
                if (m_WebResponse != null)
                    m_WebResponse.Dispose();

                base.Dispose();
            } // Dispose

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (CanRead) {
                    if (m_DownloadStream == null) {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(m_Url);
                        HttpWebResponse res = (HttpWebResponse)request.GetResponse();
                        m_DownloadStream = res.GetResponseStream();                        
                    }
                    return m_DownloadStream.Read(buffer, offset, count);
                } else {
                    throw new InvalidOperationException();
                }           
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new InvalidOperationException();
            }

            public override void SetLength(long value)
            {
                throw new InvalidOperationException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (CanWrite) {
                    m_Api.UploadFragment(m_Url, buffer, m_Pos, count, m_Size).Wait();
                    m_Pos += count;
                } else {
                    throw new InvalidOperationException();
                }
            }
        } // OneDriveStream

        private const string m_ClientId = "00000000441B5EA9";
        private const string m_ClientSecret = "7eOT0VS1frWih6KiJ4emHqS";
        private OneDriveAPI m_Api = null;

        public OneDriveFileManager(string accessToken, string refreshToken) :
            base()
        {
            m_Api = new OneDriveAPI(m_ClientId, m_ClientSecret, accessToken, refreshToken);
        }

        public string AccessToken { 
            get { return m_Api.AccessToken; }
        }

        public string RefreshToken
        {
            get { return m_Api.RefreshToken; }
        }

        public override void Dispose()
        {
            
        }

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
                    if (!res.EndsWith(GetPathSeparator()) && !path.StartsWith(GetPathSeparator()))
                        res = $"{res}/{path}";
                    else 
                        res = $"{res}{path}";
                }
            }
            return res;
        } // CombinePath

        public override async Task<bool> CreateDirectory(string path)
        {
            int idx = path.LastIndexOf(GetPathSeparator());
            string parent = path.Substring(0, idx);
            string folderName = path.Substring(idx + 1);

            if (parent.StartsWith(GetPathSeparator()))
                parent = parent.Remove(0, 1);
            var p = await m_Api.ItemMetadataFromPath(parent);
            return await m_Api.Createfolder(p.Id, folderName);
        } // CreateDirectory

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
                FullName = $"onedrive://{path}"
            });
        } // GetPositionInfo

        public override async Task<bool> Delete(string path)
        {
            path = GetPath(path);
            if (path.StartsWith(GetPathSeparator()))
                path = path.Remove(0, 1);            
            var i = await m_Api.ItemMetadataFromPath(path);
            return await m_Api.Delete(i.Id);
        } // Delete

        public override async Task<bool> DirectoryExists(string path)
        {
            path = GetPath(path);
            if (path.StartsWith(GetPathSeparator()))
                path = path.Remove(0, 1);
            var i = await m_Api.ItemMetadataFromPath(path);
            return i != null && i.Folder != null;
        } // DirectoryExists

        public override async Task<bool> FileExists(string path)
        {
            path = GetPath(path);
            var i = await m_Api.ItemMetadataFromPath(path);
            return i != null && i.Folder == null;
        } // FileExists

        public override Bookmark GetBookmark()
        {
            return new OneDriveBookmark()
            {
                AccessToken = AccessToken,
                RefreshToken = RefreshToken
            };
        } // GetBookmark

        public override async Task<bool> HasSubdirs(string position)
        {
            position = GetPath(position);
            if (position.StartsWith(GetPathSeparator()))
                position = position.Remove(0, 1);
                            
            var im = await m_Api.ItemMetadataFromPath(position);            
            var children = await m_Api.ItemChildren(im.Id);
            while (true) {
                foreach (var child in children.Children) {
                    if (child.Folder != null) {
                        return true;
                    } else {
                        return false;
                    }
                }
                if (string.IsNullOrEmpty(children.NextLink))
                    break;
                children = await m_Api.ItemChildrenNext(children.NextLink);                
            }
            return false;
        } // HasSubdirs
        public override async Task<List<FileInfo>> GetDirectoryList(string position, bool includeHidden = true)
        {
            position = GetPath(position);
            if (position.StartsWith(GetPathSeparator()))
                position = position.Remove(0, 1);

            var res = new List<FileInfo>();

            var im = await m_Api.ItemMetadataFromPath(position);            
            var children = await m_Api.ItemChildren(im.Id);
            while (true) {
                foreach (var child in children.Children) {
                    if (child.Folder != null) {
                        res.Add(new FileInfo()
                        {
                            Name = child.Name,
                            FullPath = $"onedrive://{(string.IsNullOrEmpty(position) ? $"{position}/{child.Name}" : $"/{position}/{child.Name}")}",
                            LastModified = child.LastModifiedDateTime.Value,
                            Created = child.CreatedDateTime.Value,
                            IsDirectory = child.Folder != null,
                            Type = child.Folder != null ? DirectoryMimeType : null,
                            Size = child.Size.HasValue ? child.Size.Value : 0
                        });
                    } else {
                        return res;
                    }
                }

                if (string.IsNullOrEmpty(children.NextLink))
                    break;              
                children = await m_Api.ItemChildrenNext(children.NextLink);
            }
            return res;
        } // GetDirectoryList

        public override async Task<List<DriveInfo>> GetDrives()
        {
            return new List<DriveInfo>() { await GetDriveInfo(GetRoot()) };
        } // GetDrives

        public override async Task<DriveInfo> GetDriveInfo(string path)
        {
            var drive = await m_Api.GetDefaultDrive();
            return new DriveInfo() {
                Name = "onedrive:///",                
                VolumeLabel = drive.Owner?.User.DisplayName ?? string.Empty,
                Format = drive.DriveType,
                Size = drive.Quota.Total,
                FreeSpace = drive.Quota.Remaining
            };
        } // GetDriveInfo

        public override async Task<FileInfo> GetFileInfo(string filePath)
        {
            filePath = GetPath(filePath);
            var i = await m_Api.ItemMetadataFromPath(filePath);
            return new FileInfo()
            {
                Name = i.Name,
                FullPath = $"onedrive://{filePath}",
                LastModified = i.LastModifiedDateTime.Value,
                Created = i.CreatedDateTime.Value,
                IsDirectory = i.Folder != null,
                Type = i.Folder != null ? DirectoryMimeType : null,
                Size = i.Size.HasValue ? i.Size.Value : 0
            };
        } // GetFileInfo

        public override async Task<List<FileInfo>> GetFileList(string position, CancellationToken cancellationToken, bool includeHidden = true)
        {
            position = GetPath(position);
            var res = new List<FileInfo>();
            if (position != "/") {
                int idx = position.LastIndexOf(GetPathSeparator());
                if (idx >= 0) {
                    var fi = new FileInfo()
                    {
                        Name = "..",
                        FullPath = $"onedrive://{position.Substring(0, idx)}",
                        Type = DirectoryMimeType,
                        IsDirectory = true,
                    };
                    if (string.IsNullOrEmpty(fi.FullPath))
                        fi.FullPath = GetPathSeparator();
                    res.Add(fi);
                }
            }

            if (position.StartsWith(GetPathSeparator()))
                position = position.Remove(0, 1);

            var im = await m_Api.ItemMetadataFromPath(position);            
            var children = await m_Api.ItemChildren(im.Id);
            while (true) {
                foreach (var child in children.Children) {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    res.Add(new FileInfo()
                    {
                        Name = child.Name,
                        FullPath = $"onedrive://{(string.IsNullOrEmpty(position) ? $"{position}/{child.Name}" : $"/{position}/{child.Name}")}",
                        LastModified = child.LastModifiedDateTime.Value,
                        Created = child.CreatedDateTime.Value,
                        IsDirectory = child.Folder != null,
                        Type = child.Folder != null ? DirectoryMimeType : null,
                        Size = child.Size.HasValue ? child.Size.Value : 0
                    });
                }

                if (string.IsNullOrEmpty(children.NextLink))
                    break;

                if (cancellationToken.IsCancellationRequested)
                    break;                
                children = await m_Api.ItemChildrenNext(children.NextLink);
            }

            return res;
        } // GetFileList

        public override string GetRoot()
        {
            return "onedrive:///";
        }

        public override async Task<Stream> OpenFile(string filePath, bool write = false, long? size = null)
        {
            filePath = GetPath(filePath);
            if (write) {
                int idx = filePath.LastIndexOf(GetPathSeparator());
                string folderPath = filePath.Substring(0, idx);
                string fileName = filePath.Substring(idx + 1);

                if (folderPath.StartsWith(GetPathSeparator()))
                    folderPath = folderPath.Remove(0, 1);
                var folder = await m_Api.ItemMetadataFromPath(folderPath);
                var cs = await m_Api.UploadCreateSession(folder.Id, fileName);

                return new OneDriveStream(m_Api, cs.UploadUrl, OneDriveStream.Modes.Write, size);
            } else {
                if (filePath.StartsWith(GetPathSeparator()))
                    filePath = filePath.Remove(0, 1);                
                var item = await m_Api.ItemMetadataFromPath(filePath);
                return new OneDriveStream(m_Api, item.DownloadUrl, OneDriveStream.Modes.Read);
            }
        } // OpenFile

        public override bool OpenFileWithDefaultApplication(string filePath)
        {
            throw new System.NotImplementedException();
        }

        public override async Task<bool> Rename(string path, string newName)
        {
            path = GetPath(path);
            var i = m_Api.ItemMetadataFromPath(path).Result;
            i.Name = newName;
            i = await m_Api.UpdateItem(i);
            return true;
        } // Rename

        private string GetPath(string position)
        {
            if (position.StartsWith("onedrive://"))
                return position.Remove(0, 11);
            return position;
        } // GetPath        
    }
}