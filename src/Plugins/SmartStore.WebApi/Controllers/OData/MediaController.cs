﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using SmartStore.ComponentModel;
using SmartStore.Core.Domain.Media;
using SmartStore.Core.Security;
using SmartStore.Services.Media;
using SmartStore.Web.Framework.WebApi;
using SmartStore.Web.Framework.WebApi.Configuration;
using SmartStore.Web.Framework.WebApi.OData;
using SmartStore.Web.Framework.WebApi.Security;
using SmartStore.WebApi.Models.OData.Media;

namespace SmartStore.WebApi.Controllers.OData
{
    /// <summary>
    /// Is intended to make methods of the IMediaService accessible. Direct access to the MediaFile entity is not intended.
    /// </summary>
    /// <remarks>
    /// Functions like GET /Media/FileExists(Path='content/my-file.jpg') would never work (404).
    /// That's why some endpoints are implemented as Actions (POST).
    /// </remarks>
    public class MediaController : WebApiEntityController<MediaFile, IMediaService>
    {
        public static MediaLoadFlags _defaultLoadFlags = MediaLoadFlags.AsNoTracking | MediaLoadFlags.WithTags | MediaLoadFlags.WithTracks | MediaLoadFlags.WithFolder;

        // GET /Media(123)
        [WebApiQueryable]
        [WebApiAuthenticate]
        public IHttpActionResult Get(int key)
        {
            var file = Service.GetFileById(key, _defaultLoadFlags);
            if (file == null)
            {
                return NotFound();
            }

            return Ok(Convert(file));
        }

        // GET /Media
        [WebApiQueryable]
        [WebApiAuthenticate]
        public IHttpActionResult Get(/*ODataQueryOptions<MediaFile> queryOptions*/)
        {
            return StatusCode(HttpStatusCode.NotImplemented);

            // TODO or not TODO :)
            //var maxTop = WebApiCachingControllingData.Data().MaxTop;
            //var top = Math.Min(this.GetQueryStringValue("$top", maxTop), maxTop);

            //var query = queryOptions.ApplyTo(GetEntitySet(), new ODataQuerySettings { PageSize = top }) as IQueryable<MediaFile>;
            //var files = query.ToList();
            //var result = files.Select(x => Convert(Service.ConvertMediaFile(x)));

            //return result.AsQueryable();
        }

        // GET /Media(123)/ThumbUrl
        [WebApiAuthenticate]
        public IHttpActionResult GetProperty(int key, string propertyName)
        {
            Type propertyType = null;
            object propertyValue = null;

            this.ProcessEntity(() =>
            {
                var file = Service.GetFileById(key);
                if (file == null)
                {
                    throw Request.NotFoundException(WebApiGlobal.Error.EntityNotFound.FormatInvariant(key));
                }

                var item = Convert(file);

                var prop = FastProperty.GetProperty(item.GetType(), propertyName);
                if (prop == null)
                {
                    throw Request.BadRequestException(WebApiGlobal.Error.PropertyNotFound.FormatInvariant(propertyName.EmptyNull()));
                }

                propertyType = prop.Property.PropertyType;
                propertyValue = prop.GetValue(item);
            });

            if (propertyType == null)
            {
                return StatusCode(HttpStatusCode.NoContent);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, propertyType, propertyValue);
            return ResponseMessage(response);
        }

        public IHttpActionResult Post()
        {
            return StatusCode(HttpStatusCode.Forbidden);
        }

        public IHttpActionResult Put()
        {
            return StatusCode(HttpStatusCode.Forbidden);
        }

        public IHttpActionResult Patch()
        {
            return StatusCode(HttpStatusCode.Forbidden);
        }

        public IHttpActionResult Delete()
        {
            // We do not allow direct entity deletion.
            // There is an action method "DeleteFile" instead to trigger the corresponding service method.

            return StatusCode(HttpStatusCode.Forbidden);
        }

        #region Actions and functions

        public static void Init(WebApiConfigurationBroadcaster configData)
        {
            var entityConfig = configData.ModelBuilder.EntityType<FileItemInfo>();

            #region Files

            entityConfig.Collection
                .Action("GetFileByPath")
                .ReturnsFromEntitySet<FileItemInfo>("Media")
                .Parameter<string>("Path");

            //entityConfig.Collection
            //    .Action("GetFileByName")
            //    .ReturnsFromEntitySet<FileItemInfo>("Media")
            //    .AddParameter<string>("FileName")
            //    .AddParameter<int>("FolderId");

            entityConfig.Collection
                .Function("GetFilesByIds")
                .ReturnsFromEntitySet<FileItemInfo>("Media")
                .CollectionParameter<int>("Ids");

            entityConfig.Collection
                .Action("FileExists")
                .Returns<bool>()
                .Parameter<string>("Path");

            entityConfig.Collection
                .Action("CheckUniqueFileName")
                .Returns<CheckUniqueFileNameResult>()
                .Parameter<string>("Path");

            entityConfig.Collection
                .Action("CountFiles")
                .Returns<int>()
                .Parameter<MediaSearchQuery>("Query");

            entityConfig.Collection
                .Action("CountFilesGrouped")
                .Returns<CountFilesGroupedResult>()
                .Parameter<MediaFilesFilter>("Filter");

            // Doesn't work:
            //var cfgr = configData.ModelBuilder.ComplexType<CountFilesGroupedResult>();
            //cfgr.Property(x => x.Total);
            //cfgr.Property(x => x.Trash);
            //cfgr.Property(x => x.Unassigned);
            //cfgr.Property(x => x.Transient);
            //cfgr.Property(x => x.Orphan);
            //cfgr.ComplexProperty(x => x.Filter);
            //cfgr.HasDynamicProperties(x => x.Folders);

            entityConfig
                .Action("MoveFile")
                .ReturnsFromEntitySet<FileItemInfo>("Media")
                .AddParameter<string>("DestinationFileName")
                .AddParameter<DuplicateFileHandling>("DuplicateFileHandling", true);

            entityConfig
                .Action("CopyFile")
                .ReturnsFromEntitySet<FileItemInfo>("Media")
                .AddParameter<string>("DestinationFileName")
                .AddParameter<DuplicateFileHandling>("DuplicateFileHandling", true);

            entityConfig
                .Action("DeleteFile")
                .AddParameter<bool>("Permanent")
                .AddParameter<bool>("Force", true);

            #endregion

            #region Folders

            entityConfig.Collection
                .Action("FolderExists")
                .Returns<bool>()
                .Parameter<string>("Path");

            entityConfig.Collection
                .Action("CreateFolder")
                .Returns<FolderItemInfo>()
                .Parameter<string>("Path");

            entityConfig.Collection
                .Action("MoveFolder")
                .Returns<FolderItemInfo>()
                .AddParameter<string>("Path")
                .AddParameter<string>("DestinationPath");

            entityConfig.Collection
                .Action("CopyFolder")
                .Returns<FolderItemInfo>()
                .AddParameter<string>("Path")
                .AddParameter<string>("DestinationPath")
                .AddParameter<DuplicateEntryHandling>("DuplicateEntryHandling", true);

            entityConfig.Collection
                .Action("DeleteFolder")
                .AddParameter<string>("Path")
                .AddParameter<FileHandling>("FileHandling", true);

            #endregion
        }

        /// POST /Media/GetFileByPath {"Path":"content/my-file.jpg"}
        [HttpPost]
        [WebApiAuthenticate]
        public FileItemInfo GetFileByPath(ODataActionParameters parameters)
        {
            FileItemInfo file = null;

            this.ProcessEntity(() =>
            {
                var path = parameters.GetValueSafe<string>("Path");
                var mediaFile = Service.GetFileByPath(path, _defaultLoadFlags);

                if (mediaFile == null)
                {
                    throw Request.NotFoundException($"The file with the path '{path ?? string.Empty}' does not exist.");
                }

                file = Convert(mediaFile);
            });

            return file;
        }

        // POST /Media/GetFileByName {"FolderId":2, "FileName":"my-file.jpg"}
        //[HttpPost, WebApiAuthenticate]
        //public MediaFileInfo GetFileByName(ODataActionParameters parameters)
        //{
        //    MediaFileInfo file = null;

        //    this.ProcessEntity(() =>
        //    {
        //        var folderId = parameters.GetValueSafe<int>("FolderId");
        //        var fileName = parameters.GetValueSafe<string>("FileName");

        //        file = _mediaService.GetFileByName(folderId, fileName);
        //        if (file == null)
        //        {
        //            throw this.ExceptionNotFound($"The file with the folder ID {folderId} and file name '{fileName ?? string.Empty}' does not exist.");
        //        }
        //    });

        //    return file;
        //}

        /// GET /Media/GetFilesByIds(Ids=[1,2,3])
        [HttpGet, WebApiQueryable]
        [WebApiAuthenticate]
        public IQueryable<FileItemInfo> GetFilesByIds([FromODataUri] int[] ids)
        {
            IQueryable<FileItemInfo> files = null;

            this.ProcessEntity(() =>
            {
                if (ids?.Any() ?? false)
                {
                    var mediaFiles = Service.GetFilesByIds(ids.ToArray(), _defaultLoadFlags);
                    
                    files = mediaFiles.Select(x => Convert(x)).AsQueryable();
                }
            });

            return files ??  new List<FileItemInfo>().AsQueryable();
        }

        /// POST /Media/FileExists {"Path":"content/my-file.jpg"}
        [HttpPost]
        [WebApiAuthenticate]
        public bool FileExists(ODataActionParameters parameters)
        {
            var fileExists = false;

            this.ProcessEntity(() =>
            {
                var path = parameters.GetValueSafe<string>("Path");
                fileExists = Service.FileExists(path);
            });

            return fileExists;
        }

        /// POST /Media/CheckUniqueFileName {"Path":"content/my-file.jpg"}
        [HttpPost]
        [WebApiAuthenticate]
        public CheckUniqueFileNameResult CheckUniqueFileName(ODataActionParameters parameters)
        {
            var result = new CheckUniqueFileNameResult();

            this.ProcessEntity(() =>
            {
                var path = parameters.GetValueSafe<string>("Path");

                result.Result = Service.CheckUniqueFileName(path, out string newPath);
                result.NewPath = newPath;
            });

            return result;
        }

        /// POST /Media/CountFiles {"Query":{"FolderId":7,"Extensions":["jpg"], ...}}
        [HttpPost]
        [WebApiAuthenticate]
        public async Task<int> CountFiles(ODataActionParameters parameters)
        {
            var count = 0;

            await this.ProcessEntityAsync(async () =>
            {
                var query = parameters.GetValueSafe<MediaSearchQuery>("Query");
                count = await Service.CountFilesAsync(query ?? new MediaSearchQuery());
            });

            return count;
        }

        /// POST /Media/CountFilesGrouped {"Filter":{"Term":"my image","Extensions":["jpg"], ...}}
        [HttpPost]
        [WebApiAuthenticate]
        public CountFilesGroupedResult CountFilesGrouped(ODataActionParameters parameters)
        {
            CountFilesGroupedResult result = null;

            this.ProcessEntity(() =>
            {
                var query = parameters.GetValueSafe<MediaFilesFilter>("Filter");
                var res = Service.CountFilesGrouped(query ?? new MediaFilesFilter());

                result = new CountFilesGroupedResult
                {
                    Total = res.Total,
                    Trash = res.Trash,
                    Unassigned = res.Unassigned,
                    Transient = res.Unassigned,
                    Orphan = res.Orphan,
                    Filter = res.Filter
                };

                result.Folders = res.Folders
                    .Select(x => new CountFilesGroupedResult.FolderCount
                    {
                        FolderId = x.Key,
                        Count = x.Value
                    })
                    .ToList();
            });

            return result;
        }

        /// POST /Media(123)/MoveFile {"DestinationFileName":"content/updated-file-name.jpg"}
        [HttpPost]
        [WebApiAuthenticate(Permission = Permissions.Media.Update)]
        public FileItemInfo MoveFile(int key, ODataActionParameters parameters)
        {
            FileItemInfo movedFile = null;

            this.ProcessEntity(() =>
            {
                var file = Service.GetFileById(key);
                if (file == null)
                {
                    throw Request.NotFoundException(WebApiGlobal.Error.EntityNotFound.FormatInvariant(key));
                }

                var destinationFileName = parameters.GetValueSafe<string>("DestinationFileName");
                var duplicateFileHandling = parameters.GetValueSafe("DuplicateFileHandling", DuplicateFileHandling.ThrowError);

                var result = Service.MoveFile(file.File, destinationFileName, duplicateFileHandling);
                movedFile = Convert(result); 
            });

            return movedFile;
        }

        /// POST /Media(123)/CopyFile {"DestinationFileName":"content/new-file.jpg"}
        [HttpPost]
        [WebApiAuthenticate(Permission = Permissions.Media.Update)]
        public FileItemInfo CopyFile(int key, ODataActionParameters parameters)
        {
            FileItemInfo fileCopy = null;

            this.ProcessEntity(() =>
            {
                var file = Service.GetFileById(key);
                if (file == null)
                {
                    throw Request.NotFoundException(WebApiGlobal.Error.EntityNotFound.FormatInvariant(key));
                }

                var destinationFileName = parameters.GetValueSafe<string>("DestinationFileName");
                var duplicateFileHandling = parameters.GetValueSafe("DuplicateFileHandling", DuplicateFileHandling.ThrowError);

                var result = Service.CopyFile(file, destinationFileName, duplicateFileHandling);
                fileCopy = Convert(result.DestinationFile);
            });

            return fileCopy;
        }

        /// POST /Media(123)/DeleteFile {"Permanent":false}
        [HttpPost]
        [WebApiAuthenticate(Permission = Permissions.Media.Delete)]
        public IHttpActionResult DeleteFile(int key, ODataActionParameters parameters)
        {
            this.ProcessEntity(() =>
            {
                var file = Service.GetFileById(key);
                if (file == null)
                {
                    throw Request.NotFoundException(WebApiGlobal.Error.EntityNotFound.FormatInvariant(key));
                }

                var permanent = parameters.GetValueSafe<bool>("Permanent");
                var force = parameters.GetValueSafe("Force", false);

                Service.DeleteFile(file.File, permanent, force);
            });

            return StatusCode(HttpStatusCode.NoContent);
        }


        /// POST /Media/FolderExists {"Path":"my-folder"}
        [HttpPost]
        [WebApiAuthenticate]
        public bool FolderExists(ODataActionParameters parameters)
        {
            var folderExists = false;

            this.ProcessEntity(() =>
            {
                var path = parameters.GetValueSafe<string>("Path");
                folderExists = Service.FolderExists(path);
            });

            return folderExists;
        }

        /// POST /Media/CreateFolder {"Path":"content/my-folder"}
        [HttpPost]
        [WebApiAuthenticate]
        public IHttpActionResult CreateFolder(ODataActionParameters parameters)
        {
            FolderItemInfo newFolder = null;

            this.ProcessEntity(() =>
            {
                var path = parameters.GetValueSafe<string>("Path");

                var result = Service.CreateFolder(path);
                newFolder = Convert(result);
            });

            return Response(HttpStatusCode.Created, newFolder);
        }

        /// POST /Media/MoveFolder {"Path":"content/my-folder", "DestinationPath":"content/my-renamed-folder"}
        [HttpPost]
        [WebApiAuthenticate(Permission = Permissions.Media.Update)]
        public IHttpActionResult MoveFolder(ODataActionParameters parameters)
        {
            FolderItemInfo movedFolder = null;

            this.ProcessEntity(() =>
            {
                var path = parameters.GetValueSafe<string>("Path");
                var destinationPath = parameters.GetValueSafe<string>("DestinationPath");

                var result = Service.MoveFolder(path, destinationPath);
                movedFolder = Convert(result);
            });

            return Ok(movedFolder);
        }

        /// POST /Media/CopyFolder {"Path":"content/my-folder", "DestinationPath":"content/my-new-folder"}
        [HttpPost]
        [WebApiAuthenticate(Permission = Permissions.Media.Update)]
        public IHttpActionResult CopyFolder(ODataActionParameters parameters)
        {
            FolderItemInfo copiedFolder = null;

            this.ProcessEntity(() =>
            {
                var path = parameters.GetValueSafe<string>("Path");
                var destinationPath = parameters.GetValueSafe<string>("DestinationPath");
                var duplicateEntryHandling = parameters.GetValueSafe("DuplicateEntryHandling", DuplicateEntryHandling.Skip);

                var result = Service.CopyFolder(path, destinationPath, duplicateEntryHandling);
                copiedFolder = Convert(result.Folder);
            });

            return Ok(copiedFolder);
        }

        /// POST /Media/DeleteFolder {"Path":"content/my-folder"}
        [HttpPost]
        [WebApiAuthenticate(Permission = Permissions.Media.Delete)]
        public IHttpActionResult DeleteFolder(ODataActionParameters parameters)
        {
            this.ProcessEntity(() =>
            {
                var path = parameters.GetValueSafe<string>("Path");
                var fileHandling = parameters.GetValueSafe("FileHandling", FileHandling.SoftDelete);

                Service.DeleteFolder(path, fileHandling);
            });

            return StatusCode(HttpStatusCode.NoContent);
        }

        #endregion

        #region Utilities

        private FileItemInfo Convert(MediaFileInfo file)
        {
            var item = MiniMapper.Map<MediaFileInfo, FileItemInfo>(file, CultureInfo.InvariantCulture);
            return item;
        }

        private FolderItemInfo Convert(MediaFolderInfo folder)
        {
            var item = MiniMapper.Map<MediaFolderInfo, FolderItemInfo>(folder, CultureInfo.InvariantCulture);
            return item;
        }

        #endregion
    }


    //   public class PicturesController : WebApiEntityController<MediaFile, IMediaService>
    //{
    //       protected override IQueryable<MediaFile> GetEntitySet()
    //       {
    //           var query =
    //               from x in Repository.Table
    //               where !x.Deleted && !x.Hidden
    //               select x;

    //           return query;
    //       }

    //	[WebApiQueryable]
    //       public SingleResult<MediaFile> GetPicture(int key)
    //	{
    //		return GetSingleResult(key);
    //	}

    //	[WebApiQueryable]
    //       public IQueryable<ProductMediaFile> GetProductPictures(int key)
    //	{
    //		return GetRelatedCollection(key, x => x.ProductMediaFiles);
    //	}
    //}
}
