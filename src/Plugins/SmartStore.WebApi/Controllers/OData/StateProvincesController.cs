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
    public class StateProvincesController : WebApiEntityController<StateProvince, IStateProvinceService>
	{
		[WebApiQueryable]
		[WebApiAuthenticate(Permission = Permissions.Configuration.Country.Read)]
		public IQueryable<StateProvince> Get()
		{
			return GetEntitySet();
		}

		[WebApiQueryable]
        [WebApiAuthenticate(Permission = Permissions.Configuration.Country.Read)]
        public SingleResult<StateProvince> Get(int key)
		{
			return GetSingleResult(key);
		}

		[WebApiAuthenticate(Permission = Permissions.Configuration.Country.Create)]
		public IHttpActionResult Post(StateProvince entity)
		{
			var result = Insert(entity, () => Service.InsertStateProvince(entity));
			return result;
		}

		[WebApiAuthenticate(Permission = Permissions.Configuration.Country.Update)]
		public async Task<IHttpActionResult> Put(int key, StateProvince entity)
		{
			var result = await UpdateAsync(entity, key, () => Service.UpdateStateProvince(entity));
			return result;
		}

		[WebApiAuthenticate(Permission = Permissions.Configuration.Country.Update)]
		public async Task<IHttpActionResult> Patch(int key, Delta<StateProvince> model)
		{
			var result = await PartiallyUpdateAsync(key, model, entity => Service.UpdateStateProvince(entity));
			return result;
		}

		[WebApiAuthenticate(Permission = Permissions.Configuration.Country.Delete)]
		public async Task<IHttpActionResult> Delete(int key)
		{
			var result = await DeleteAsync(key, entity => Service.DeleteStateProvince(entity));
			return result;
		}

		#region Navigation properties

		[WebApiQueryable]
        [WebApiAuthenticate(Permission = Permissions.Configuration.Country.Read)]
        public SingleResult<Country> GetCountry(int key)
		{
			return GetRelatedEntity(key, x => x.Country);
		}

		#endregion
	}
}
