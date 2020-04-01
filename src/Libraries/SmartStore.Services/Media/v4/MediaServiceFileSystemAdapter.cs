﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartStore.Collections;
using SmartStore.Core.Data;
using SmartStore.Core.Domain.Media;
using SmartStore.Core.IO;
using SmartStore.Services.Media.Storage;

namespace SmartStore.Services.Media
{
    public partial class MediaServiceFileSystemAdapter : IMediaFileSystem
    {
        private readonly IMediaService _mediaService;
        private readonly MediaHelper _mediaHelper;
        private readonly IFolderService _folderService;
        private readonly IMediaStorageProvider _storageProvider;
        private readonly string _mediaRootPath;

        public MediaServiceFileSystemAdapter(
            IMediaService mediaService, 
            IFolderService folderService,
            MediaHelper mediaHelper)
        {
            _mediaService = mediaService;
            _folderService = folderService;
            _mediaHelper = mediaHelper;
            _storageProvider = mediaService.StorageProvider;
            _mediaRootPath = "media4/"; // MediaFileSystem.GetMediaPublicPath(); // TODO: (mm) switch
        }

        protected string Fix(string path)
        {
            return path.Replace('\\', '/');
        }

        #region IFileSystem

        public bool IsCloudStorage
        {
            get => _storageProvider.IsCloudStorage;
        }

        public string Root => string.Empty;

        public string GetPublicUrl(IFile file, bool forCloud = false)
        {
            if (file is MediaFileInfo mediaFile)
            {
                return _mediaService.GetUrl(mediaFile, null, string.Empty);
            }

            throw new ArgumentException("Type of file must be '{0}'.".FormatInvariant(typeof(MediaFileInfo).FullName), nameof(file));
        }

        public string GetPublicUrl(string path, bool forCloud = false)
        {
            throw new NotSupportedException();
        }

        public string GetStoragePath(string url)
        {
            url = Fix(url).TrimStart('/');

            if (!url.StartsWith(_mediaRootPath, StringComparison.OrdinalIgnoreCase))
            {
                // Is a folder path, no need to strip off public URL stuff.
                return url;
            }
            
            // Strip off root, e.g. "media/"
            var path = url.Substring(_mediaRootPath.Length);

            // Strip off media id from path, e.g. "123/"
            var firstSlashIndex = path.IndexOf('/');
            
            return path.Substring(firstSlashIndex);
        }

        public string Combine(string path1, string path2)
        {
            return Fix(Path.Combine(path1, path2));
        }

        public IEnumerable<IFile> ListFiles(string path)
        {
            var node = _folderService.GetNodeByPath(path);
            if (node == null)
            {
                throw new MediaFolderNotFoundException(path);
            }

            var query = new MediaSearchQuery
            {
                FolderId = node.Value.Id,
                Deleted = false
            };

            return _mediaService.SearchFiles(query);
        }

        public IEnumerable<IFolder> ListFolders(string path)
        {
            var node = _folderService.GetNodeByPath(path);
            if (node == null)
            {
                throw new MediaFolderNotFoundException(path);
            }

            return node.Children.Select(x => new MediaFolderInfo(x));
        }

        public long CountFiles(string path, string pattern, Func<string, bool> predicate, bool deep = true)
        {
            if (predicate == null)
            {
                var node = _folderService.GetNodeByPath(path);
                if (node == null)
                {
                    throw new MediaFolderNotFoundException(path);
                }

                var query = new MediaSearchQuery
                {
                    FolderId = node.Value.Id,
                    DeepSearch = deep,
                    Term = pattern,
                    Deleted = false
                };

                return _mediaService.CountFiles(query);
            }

            var files = SearchFiles(path, pattern, deep);
            return files.Count(predicate);
        }

        public IEnumerable<string> SearchFiles(string path, string pattern, bool deep = true)
        {
            var node = _folderService.GetNodeByPath(path);
            if (node == null)
            {
                throw new MediaFolderNotFoundException(path);
            }
            
            var query = new MediaSearchQuery
            {
                FolderId = node.Value.Id,
                DeepSearch = deep,
                Term = pattern,
                Deleted = false
            };

            return _mediaService.SearchFiles(query).Select(x => x.Path).ToList();
        }

        public IFile CreateFile(string path)
        {
            return _mediaService.SaveFile(path, null, false, false);
        }

        public Task<IFile> CreateFileAsync(string path)
        {
            return Task.FromResult(CreateFile(path));
        }

        public void CreateFolder(string path)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile(string path)
        {
            var file = _mediaService.GetFileByPath(path);
            if (file?.Exists == true)
            {
                _mediaService.DeleteFile((MediaFile)file, false);
            }
        }

        public void DeleteFolder(string path)
        {
            var node = _folderService.GetNodeByPath(path);
            if (node != null)
            {
                var folder = _folderService.GetFolderById(node.Value.Id);
                if (folder != null)
                {
                    _folderService.DeleteFolder(folder);
                }
            }
        }

        public bool FileExists(string path)
        {
            return _mediaService.FileExists(path);
        }

        public bool FolderExists(string path)
        {
            return _folderService.FolderExists(path);
        }

        public IFile GetFile(string path)
        {
            var file = _mediaService.GetFileByPath(path);
            if (file == null)
            {
                var mediaFile = new MediaFile 
                { 
                    Name = Path.GetFileName(path),
                    Extension = Path.GetExtension(path).TrimStart('.'),
                    MimeType = MimeTypes.MapNameToMimeType(path)
                };
                file = new MediaFileInfo(mediaFile, null, Fix(Path.GetDirectoryName(path)));
            }

            return file;
        }

        public IFolder GetFolder(string path)
        {
            var node = _folderService.GetNodeByPath(path);
            if (node == null)
            {
                node = new TreeNode<MediaFolderNode>(new MediaFolderNode { Path = path, Name = Path.GetFileName(Fix(path)) });
            }

            return new MediaFolderInfo(node);
        }

        public IFolder GetFolderForFile(string path)
        {
            if (!_mediaHelper.TokenizePath(path, out var pathData))
            {
                throw new MediaFolderNotFoundException(Fix(Path.GetDirectoryName(path)));
            }

            return new MediaFolderInfo(pathData.Node);
        }

        public bool CheckUniqueFileName(string path, out string newPath)
        {
            return _mediaService.CheckUniqueFileName(path, out newPath);
        }

        public void CopyFile(string path, string newPath, bool overwrite = false)
        {
            Guard.NotEmpty(path, nameof(path));
            Guard.NotEmpty(newPath, nameof(newPath));

            var sourceFile = (MediaFile)_mediaService.GetFileByPath(path);
            if (sourceFile == null)
            {
                throw new MediaFileNotFoundException(path);
            }

            _mediaService.CopyFile(sourceFile, newPath, false);
        }

        public void RenameFile(string path, string newPath)
        {
            Guard.NotEmpty(path, nameof(path));
            Guard.NotEmpty(newPath, nameof(newPath));

            var sourceFile = (MediaFile)_mediaService.GetFileByPath(path);
            if (sourceFile == null)
            {
                throw new MediaFileNotFoundException(path);
            }

            _mediaService.MoveFile(sourceFile, newPath);
        }

        public void RenameFolder(string path, string newPath)
        {
            Guard.NotEmpty(path, nameof(path));
            Guard.NotEmpty(newPath, nameof(newPath));

            var sourceNode = _folderService.GetNodeByPath(path);
            if (sourceNode == null)
            {
                throw new MediaFolderNotFoundException(path);
            }

            var sourceFolder = _folderService.GetFolderById(sourceNode.Value.Id);
            if (sourceFolder == null)
            {
                throw new MediaFolderNotFoundException(path);
            }

            _folderService.MoveFolder(sourceFolder, newPath);
        }

        public void SaveStream(string path, Stream inputStream)
        {
            _mediaService.SaveFile(path, inputStream, false, true);
        }

        public async Task SaveStreamAsync(string path, Stream inputStream)
        {
            await _mediaService.SaveFileAsync(path, inputStream, false, true);
        }

        #endregion
    }
}
