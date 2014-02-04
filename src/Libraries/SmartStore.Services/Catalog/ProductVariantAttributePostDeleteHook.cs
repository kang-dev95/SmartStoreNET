﻿using SmartStore.Core.Data.Hooks;
using SmartStore.Core.Domain.Catalog;

namespace SmartStore.Services.Catalog
{
	public class ProductVariantAttributePostDeleteHook : PostDeleteHook<ProductVariantAttribute>
	{
		private readonly IProductAttributeService _productAttributeService;

		public ProductVariantAttributePostDeleteHook(IProductAttributeService productAttributeService)
		{
			_productAttributeService = productAttributeService;
		}

		public override void Hook(ProductVariantAttribute entity, HookEntityMetadata metadata)
		{
			if (entity != null)
			{
				_productAttributeService.DeleteProductBundleItemAttributeFilter(entity.Id);
			}
		}
	}
}
