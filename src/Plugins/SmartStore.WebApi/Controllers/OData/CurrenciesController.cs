﻿using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using SmartStore.Core.Domain.Directory;
using SmartStore.Core.Security;
using SmartStore.Services.Directory;
using SmartStore.Web.Framework.WebApi;
using SmartStore.Web.Framework.WebApi.OData;
using SmartStore.Web.Framework.WebApi.Security;

namespace SmartStore.WebApi.Controllers.OData
{
    public class CurrenciesController : WebApiEntityController<Currency, ICurrencyService>
	{
		[WebApiQueryable]
		[WebApiAuthenticate(Permission = Permissions.Configuration.Currency.Read)]
		public IQueryable<Currency> Get()
		{
			return GetEntitySet();
		}

		[WebApiQueryable]
		[WebApiAuthenticate(Permission = Permissions.Configuration.Currency.Read)]
		public SingleResult<Currency> Get(int key)
		{
			return GetSingleResult(key);
		}

		[WebApiAuthenticate(Permission = Permissions.Configuration.Currency.Read)]
		public IHttpActionResult GetProperty(int key, string propertyName)
		{
			return GetPropertyValue(key, propertyName);
		}

		[WebApiAuthenticate(Permission = Permissions.Configuration.Currency.Create)]
		public IHttpActionResult Post(Currency entity)
		{
			var result = Insert(entity, () => Service.InsertCurrency(entity));
			return result;
		}

		[WebApiAuthenticate(Permission = Permissions.Configuration.Currency.Update)]
		public async Task<IHttpActionResult> Put(int key, Currency entity)
		{
			var result = await UpdateAsync(entity, key, () => Service.UpdateCurrency(entity));
			return result;
		}

		[WebApiAuthenticate(Permission = Permissions.Configuration.Currency.Update)]
		public async Task<IHttpActionResult> Patch(int key, Delta<Currency> model)
		{
			var result = await PartiallyUpdateAsync(key, model, entity => Service.UpdateCurrency(entity));
			return result;
		}

		[WebApiAuthenticate(Permission = Permissions.Configuration.Currency.Delete)]
		public async Task<IHttpActionResult> Delete(int key)
		{
			var result = await DeleteAsync(key, entity => Service.DeleteCurrency(entity));
			return result;
		}
	}
}
