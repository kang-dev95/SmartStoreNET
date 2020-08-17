﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using SmartStore.Core.Domain.Blogs;
using SmartStore.Core.Domain.Localization;
using SmartStore.Core.Security;
using SmartStore.Services.Blogs;
using SmartStore.Services.Seo;
using SmartStore.Web.Framework.WebApi;
using SmartStore.Web.Framework.WebApi.OData;
using SmartStore.Web.Framework.WebApi.Security;

namespace SmartStore.WebApi.Controllers.OData
{
    public class BlogPostsController : WebApiEntityController<BlogPost, IBlogService>
	{
		private readonly Lazy<IUrlRecordService> _urlRecordService;

		public BlogPostsController(Lazy<IUrlRecordService> urlRecordService)
		{
			_urlRecordService = urlRecordService;
		}

		protected override IQueryable<BlogPost> GetEntitySet()
		{
			var query =
				from x in this.Repository.Table
				orderby x.CreatedOnUtc descending
				select x;

			return query;
		}

		[WebApiQueryable]
		[WebApiAuthenticate(Permission = Permissions.Cms.Blog.Read)]
		public IQueryable<BlogPost> Get()
		{
			return GetEntitySet();
		}

		[WebApiQueryable]
        [WebApiAuthenticate(Permission = Permissions.Cms.Blog.Read)]
        public SingleResult<BlogPost> Get(int key)
		{
			return GetSingleResult(key);
		}

		[WebApiAuthenticate(Permission = Permissions.Cms.Blog.Read)]
		public IHttpActionResult GetProperty(int key, string propertyName)
		{
			return GetPropertyValue(key, propertyName);
		}

		[WebApiAuthenticate(Permission = Permissions.Cms.Blog.Create)]
		public IHttpActionResult Post(BlogPost entity)
		{
			var result = Insert(entity, () =>
			{
				Service.InsertBlogPost(entity);

				this.ProcessEntity(() =>
				{
					_urlRecordService.Value.SaveSlug(entity, x => x.Title);
				});
			});

			return result;
		}

		[WebApiAuthenticate(Permission = Permissions.Cms.Blog.Update)]
		public async Task<IHttpActionResult> Put(int key, BlogPost entity)
		{
			var result = await UpdateAsync(entity, key, () =>
			{
				Service.UpdateBlogPost(entity);

				this.ProcessEntity(() =>
				{
					_urlRecordService.Value.SaveSlug(entity, x => x.Title);
				});
			});

			return result;
		}

		[WebApiAuthenticate(Permission = Permissions.Cms.Blog.Update)]
		public async Task<IHttpActionResult> Patch(int key, Delta<BlogPost> model)
		{
			var result = await PartiallyUpdateAsync(key, model, entity =>
			{
				Service.UpdateBlogPost(entity);

				this.ProcessEntity(() =>
				{
					_urlRecordService.Value.SaveSlug(entity, x => x.Title);
				});
			});

			return result;
		}

		[WebApiAuthenticate(Permission = Permissions.Cms.Blog.Delete)]
		public async Task<IHttpActionResult> Delete(int key)
		{
			var result = await DeleteAsync(key, entity =>
			{
				Service.DeleteBlogPost(entity);
			});

			return result;
		}

		#region Navigation properties

		[WebApiQueryable]
        [WebApiAuthenticate(Permission = Permissions.Cms.Blog.Read)]
        public SingleResult<Language> GetLanguage(int key)
		{
			return GetRelatedEntity(key, x => x.Language);
		}

		[WebApiQueryable]
        [WebApiAuthenticate(Permission = Permissions.Cms.Blog.Read)]
        public IQueryable<BlogComment> GetBlogComments(int key)
		{
			return GetRelatedCollection(key, x => x.BlogComments);
		}

        #endregion
    }
}