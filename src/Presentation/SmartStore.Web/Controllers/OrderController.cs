﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;
using SmartStore.Core.Data;
using SmartStore.Core.Domain.Catalog;
using SmartStore.Core.Domain.Common;
using SmartStore.Core.Domain.Orders;
using SmartStore.Core.Domain.Shipping;
using SmartStore.Core.Domain.Tax;
using SmartStore.Core.Html;
using SmartStore.Core.Localization;
using SmartStore.Services;
using SmartStore.Services.Catalog;
using SmartStore.Services.Directory;
using SmartStore.Services.Helpers;
using SmartStore.Services.Localization;
using SmartStore.Services.Media;
using SmartStore.Services.Orders;
using SmartStore.Services.Payments;
using SmartStore.Services.Pdf;
using SmartStore.Services.Security;
using SmartStore.Services.Seo;
using SmartStore.Services.Shipping;
using SmartStore.Services.Stores;
using SmartStore.Web.Framework.Controllers;
using SmartStore.Web.Framework.Pdf;
using SmartStore.Web.Framework.Plugins;
using SmartStore.Web.Framework.Security;
using SmartStore.Web.Models.Order;

namespace SmartStore.Web.Controllers
{
    public partial class OrderController : PublicControllerBase
    {
		#region Fields

        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IPaymentService _paymentService;
		private readonly IPdfConverter _pdfConverter;
        private readonly IShippingService _shippingService;
        private readonly ICountryService _countryService;
		private readonly IProductService _productService;
		private readonly IProductAttributeFormatter _productAttributeFormatter;
		private readonly IStoreService _storeService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
		private readonly PluginMediator _pluginMediator;
		private readonly ICommonServices _services;
        private readonly IQuantityUnitService _quantityUnitService;

        #endregion

		#region Constructors

        public OrderController(
			IOrderService orderService, 
            IShipmentService shipmentService,
            ICurrencyService currencyService, 
			IPriceFormatter priceFormatter,
            IOrderProcessingService orderProcessingService, 
			IDateTimeHelper dateTimeHelper,
            IPaymentService paymentService,
			IPdfConverter pdfConverter, 
			IShippingService shippingService,
            ICountryService countryService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
			IStoreService storeService,
			IProductService productService,
			IProductAttributeFormatter productAttributeFormatter,
			Lazy<IPictureService> pictureService,
			PluginMediator pluginMediator,
			ICommonServices services,
            IQuantityUnitService quantityUnitService)
        {
            this._orderService = orderService;
            this._shipmentService = shipmentService;
            this._currencyService = currencyService;
            this._priceFormatter = priceFormatter;
            this._orderProcessingService = orderProcessingService;
            this._dateTimeHelper = dateTimeHelper;
            this._paymentService = paymentService;
			this._pdfConverter = pdfConverter;
            this._shippingService = shippingService;
            this._countryService = countryService;
			this._productService = productService;
			this._productAttributeFormatter = productAttributeFormatter;
			this._storeService = storeService;
            this._checkoutAttributeFormatter = checkoutAttributeFormatter;
			this._pluginMediator = pluginMediator;
			this._services = services;
            this._quantityUnitService = quantityUnitService;
        }

        #endregion

        #region Utilities

        [NonAction]
		protected OrderDetailsModel PrepareOrderDetailsModel(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");		

			var store = _storeService.GetStoreById(order.StoreId) ?? _services.StoreContext.CurrentStore;

			var orderSettings = _services.Settings.LoadSetting<OrderSettings>(store.Id);
			var catalogSettings = _services.Settings.LoadSetting<CatalogSettings>(store.Id);
			var taxSettings = _services.Settings.LoadSetting<TaxSettings>(store.Id);
			var pdfSettings = _services.Settings.LoadSetting<PdfSettings>(store.Id);
			var addressSettings = _services.Settings.LoadSetting<AddressSettings>(store.Id);
			var companyInfoSettings = _services.Settings.LoadSetting<CompanyInformationSettings>(store.Id);

			var model = new OrderDetailsModel();

			model.MerchantCompanyInfo = companyInfoSettings;
            model.Id = order.Id;
			model.StoreId = order.StoreId;
            model.OrderNumber = order.GetOrderNumber();
            model.CreatedOn = _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc);
            model.OrderStatus = order.OrderStatus.GetLocalizedEnum(_services.Localization, _services.WorkContext);
			model.IsReOrderAllowed = orderSettings.IsReOrderAllowed;
            model.IsReturnRequestAllowed = _orderProcessingService.IsReturnRequestAllowed(order);
			model.DisplayPdfInvoice = pdfSettings.Enabled;
			model.RenderOrderNotes = pdfSettings.RenderOrderNotes;

            //shipping info
			model.ShippingStatus = order.ShippingStatus.GetLocalizedEnum(_services.Localization, _services.WorkContext);
            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                model.IsShippable = true;
				model.ShippingAddress.PrepareModel(order.ShippingAddress, false, addressSettings);
                model.ShippingMethod = order.ShippingMethod;
   

                //shipments (only already shipped)
                var shipments = order.Shipments.Where(x => x.ShippedDateUtc.HasValue).OrderBy(x => x.CreatedOnUtc).ToList();
                foreach (var shipment in shipments)
                {
                    var shipmentModel = new OrderDetailsModel.ShipmentBriefModel
                    {
                        Id = shipment.Id,
                        TrackingNumber = shipment.TrackingNumber,
                    };
                    if (shipment.ShippedDateUtc.HasValue)
                        shipmentModel.ShippedDate = _dateTimeHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc);
                    if (shipment.DeliveryDateUtc.HasValue)
                        shipmentModel.DeliveryDate = _dateTimeHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc);
                    model.Shipments.Add(shipmentModel);
                }
            }


            //billing info
			model.BillingAddress.PrepareModel(order.BillingAddress, false, addressSettings);

            //VAT number
            model.VatNumber = order.VatNumber;

            //payment method
            var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(order.PaymentMethodSystemName);
			model.PaymentMethod = paymentMethod != null ? _pluginMediator.GetLocalizedFriendlyName(paymentMethod.Metadata) : order.PaymentMethodSystemName;
            model.CanRePostProcessPayment = _paymentService.CanRePostProcessPayment(order);

            //purchase order number (we have to find a better to inject this information because it's related to a certain plugin)
            if (paymentMethod != null && paymentMethod.Metadata.SystemName.Equals("SmartStore.PurchaseOrderNumber", StringComparison.InvariantCultureIgnoreCase))
            {
                model.DisplayPurchaseOrderNumber = true;
                model.PurchaseOrderNumber = order.PurchaseOrderNumber;
            }


            // totals
            switch (order.CustomerTaxDisplayType)
            {
                case TaxDisplayType.ExcludingTax:
                    {
                        //order subtotal
                        var orderSubtotalExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubtotalExclTax, order.CurrencyRate);
						model.OrderSubtotal = _priceFormatter.FormatPrice(orderSubtotalExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, false, false);
                        
						//discount (applied to order subtotal)
                        var orderSubTotalDiscountExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubTotalDiscountExclTax, order.CurrencyRate);
						if (orderSubTotalDiscountExclTaxInCustomerCurrency > decimal.Zero)
						{
							model.OrderSubTotalDiscount = _priceFormatter.FormatPrice(-orderSubTotalDiscountExclTaxInCustomerCurrency, true, 
								order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, false, false);
						}
                        
						//order shipping
                        var orderShippingExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderShippingExclTax, order.CurrencyRate);
						model.OrderShipping = _priceFormatter.FormatShippingPrice(orderShippingExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, false, false);
                        //payment method additional fee
                        var paymentMethodAdditionalFeeExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeExclTax, order.CurrencyRate);
                        if (paymentMethodAdditionalFeeExclTaxInCustomerCurrency != decimal.Zero)
							model.PaymentMethodAdditionalFee = _priceFormatter.FormatPaymentMethodAdditionalFee(paymentMethodAdditionalFeeExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, false, false);
                    }
                    break;

                case TaxDisplayType.IncludingTax:
                    {
                        //order subtotal
                        var orderSubtotalInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubtotalInclTax, order.CurrencyRate);
						model.OrderSubtotal = _priceFormatter.FormatPrice(orderSubtotalInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, true, false);
                        
						//discount (applied to order subtotal)
                        var orderSubTotalDiscountInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubTotalDiscountInclTax, order.CurrencyRate);
						if (orderSubTotalDiscountInclTaxInCustomerCurrency > decimal.Zero)
						{
							model.OrderSubTotalDiscount = _priceFormatter.FormatPrice(-orderSubTotalDiscountInclTaxInCustomerCurrency, true,
								order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, true, false);
						}

                        //order shipping
                        var orderShippingInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderShippingInclTax, order.CurrencyRate);
						model.OrderShipping = _priceFormatter.FormatShippingPrice(orderShippingInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, true, false);
                        
						//payment method additional fee
                        var paymentMethodAdditionalFeeInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeInclTax, order.CurrencyRate);
                        if (paymentMethodAdditionalFeeInclTaxInCustomerCurrency != decimal.Zero)
							model.PaymentMethodAdditionalFee = _priceFormatter.FormatPaymentMethodAdditionalFee(paymentMethodAdditionalFeeInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, true, false);
                    }
                    break;
            }

            //tax
            bool displayTax = true;
            bool displayTaxRates = true;
			if (taxSettings.HideTaxInOrderSummary && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
            {
                displayTax = false;
                displayTaxRates = false;
            }
            else
            {
				if (order.OrderTax == 0 && taxSettings.HideZeroTax)
                {
                    displayTax = false;
                    displayTaxRates = false;
                }
                else
                {
					displayTaxRates = taxSettings.DisplayTaxRates && order.TaxRatesDictionary.Count > 0;
                    displayTax = !displayTaxRates;

                    var orderTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTax, order.CurrencyRate);
                    //TODO pass languageId to _priceFormatter.FormatPrice
					model.Tax = _priceFormatter.FormatPrice(orderTaxInCustomerCurrency, true, order.CustomerCurrencyCode, false, _services.WorkContext.WorkingLanguage);
                    foreach (var tr in order.TaxRatesDictionary)
                    {
						var rate = _priceFormatter.FormatTaxRate(tr.Key);
						var labelKey = "ShoppingCart.Totals.TaxRateLine" + (_services.WorkContext.TaxDisplayType == TaxDisplayType.IncludingTax ? "Incl" : "Excl");
						model.TaxRates.Add(new OrderDetailsModel.TaxRate()
                        {
                            Rate = rate,
							Label = T(labelKey).Text.FormatCurrent(rate),
                            //TODO pass languageId to _priceFormatter.FormatPrice
							Value = _priceFormatter.FormatPrice(_currencyService.ConvertCurrency(tr.Value, order.CurrencyRate), true, order.CustomerCurrencyCode, false, _services.WorkContext.WorkingLanguage),
                        });
                    }
                }
            }
            model.DisplayTaxRates = displayTaxRates;
            model.DisplayTax = displayTax;


            //discount (applied to order total)
            var orderDiscountInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderDiscount, order.CurrencyRate);
            if (orderDiscountInCustomerCurrency > decimal.Zero)
				model.OrderTotalDiscount = _priceFormatter.FormatPrice(-orderDiscountInCustomerCurrency, true, order.CustomerCurrencyCode, false, _services.WorkContext.WorkingLanguage);


            //gift cards
            foreach (var gcuh in order.GiftCardUsageHistory)
            {
                model.GiftCards.Add(new OrderDetailsModel.GiftCard
                {
                    CouponCode = gcuh.GiftCard.GiftCardCouponCode,
					Amount = _priceFormatter.FormatPrice(-(_currencyService.ConvertCurrency(gcuh.UsedValue, order.CurrencyRate)), true, order.CustomerCurrencyCode, false, _services.WorkContext.WorkingLanguage),
                });
            }

            //reward points           
            if (order.RedeemedRewardPointsEntry != null)
            {
                model.RedeemedRewardPoints = -order.RedeemedRewardPointsEntry.Points;
				model.RedeemedRewardPointsAmount = _priceFormatter.FormatPrice(-(_currencyService.ConvertCurrency(order.RedeemedRewardPointsEntry.UsedAmount, order.CurrencyRate)), true, order.CustomerCurrencyCode, false, _services.WorkContext.WorkingLanguage);
            }

            //total
            var orderTotalInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTotal, order.CurrencyRate);
			model.OrderTotal = _priceFormatter.FormatPrice(orderTotalInCustomerCurrency, true, order.CustomerCurrencyCode, false, _services.WorkContext.WorkingLanguage);

            //checkout attributes
            model.CheckoutAttributeInfo = HtmlUtils.ConvertPlainTextToTable(HtmlUtils.ConvertHtmlToPlainText(order.CheckoutAttributeDescription));
            
            //order notes
            foreach (var orderNote in order.OrderNotes
                .Where(on => on.DisplayToCustomer)
                .OrderByDescending(on => on.CreatedOnUtc)
                .ToList())
            {
                model.OrderNotes.Add(new OrderDetailsModel.OrderNote
                {
                    Note = orderNote.FormatOrderNoteText(),
                    CreatedOn = _dateTimeHelper.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc)
                });
            }


            //purchased products
			model.ShowSku = catalogSettings.ShowProductSku;
            var orderItems = _orderService.GetAllOrderItems(order.Id, null, null, null, null, null, null);

            foreach (var orderItem in orderItems)
            {
				var orderItemModel = PrepareOrderItemModel(order, orderItem);

                model.Items.Add(orderItemModel);
            }

            return model;
        }

        [NonAction]
        protected ShipmentDetailsModel PrepareShipmentDetailsModel(Shipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException("shipment");

            var order = shipment.Order;
            if (order == null)
                throw new Exception("order cannot be loaded");

			var store = _storeService.GetStoreById(order.StoreId) ?? _services.StoreContext.CurrentStore;
			var catalogSettings = _services.Settings.LoadSetting<CatalogSettings>(store.Id);
			var shippingSettings = _services.Settings.LoadSetting<ShippingSettings>(store.Id);

            var model = new ShipmentDetailsModel();
            
            model.Id = shipment.Id;
            if (shipment.ShippedDateUtc.HasValue)
                model.ShippedDate = _dateTimeHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc);
            if (shipment.DeliveryDateUtc.HasValue)
                model.DeliveryDate = _dateTimeHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc);
            
            //tracking number and shipment information
            model.TrackingNumber = shipment.TrackingNumber;
            var srcm = _shippingService.LoadShippingRateComputationMethodBySystemName(order.ShippingRateComputationMethodSystemName);
            if (srcm != null && srcm.IsShippingRateComputationMethodActive(shippingSettings))
            {
                var shipmentTracker = srcm.Value.ShipmentTracker;
                if (shipmentTracker != null)
                {
                    model.TrackingNumberUrl = shipmentTracker.GetUrl(shipment.TrackingNumber);
					if (shippingSettings.DisplayShipmentEventsToCustomers)
                    {
                        var shipmentEvents = shipmentTracker.GetShipmentEvents(shipment.TrackingNumber);
                        if (shipmentEvents != null)
                            foreach (var shipmentEvent in shipmentEvents)
                            {
                                var shipmentStatusEventModel = new ShipmentDetailsModel.ShipmentStatusEventModel();
                                var shipmentEventCountry = _countryService.GetCountryByTwoLetterIsoCode(shipmentEvent.CountryCode);
                                shipmentStatusEventModel.Country = shipmentEventCountry != null
                                                                       ? shipmentEventCountry.GetLocalized(x => x.Name)
                                                                       : shipmentEvent.CountryCode;
                                shipmentStatusEventModel.Date = shipmentEvent.Date;
                                shipmentStatusEventModel.EventName = shipmentEvent.EventName;
                                shipmentStatusEventModel.Location = shipmentEvent.Location;
                                model.ShipmentStatusEvents.Add(shipmentStatusEventModel);
                            }
                    }
                }
            }
            
            //products in this shipment
			model.ShowSku = catalogSettings.ShowProductSku;
            foreach (var shipmentItem in shipment.ShipmentItems)
            {
                var orderItem = _orderService.GetOrderItemById(shipmentItem.OrderItemId);
                if (orderItem == null)
                    continue;

                orderItem.Product.MergeWithCombination(orderItem.AttributesXml);
                var shipmentItemModel = new ShipmentDetailsModel.ShipmentItemModel()
                {
                    Id = shipmentItem.Id,
                    Sku = orderItem.Product.Sku,
                    ProductId = orderItem.Product.Id,
					ProductName = orderItem.Product.GetLocalized(x => x.Name),
                    ProductSeName = orderItem.Product.GetSeName(),
                    AttributeInfo = orderItem.AttributeDescription,
                    QuantityOrdered = orderItem.Quantity,
                    QuantityShipped = shipmentItem.Quantity,
                };
                model.Items.Add(shipmentItemModel);
            }

            //order details model
            model.Order = PrepareOrderDetailsModel(order);
            
            return model;
        }

		private OrderDetailsModel.OrderItemModel PrepareOrderItemModel(Order order, OrderItem orderItem)
		{
			orderItem.Product.MergeWithCombination(orderItem.AttributesXml);

			var model = new OrderDetailsModel.OrderItemModel()
			{
				Id = orderItem.Id,
				Sku = orderItem.Product.Sku,
				ProductId = orderItem.Product.Id,
				ProductName = orderItem.Product.GetLocalized(x => x.Name),
				ProductSeName = orderItem.Product.GetSeName(),
				ProductType = orderItem.Product.ProductType,
				Quantity = orderItem.Quantity,
				AttributeInfo = orderItem.AttributeDescription
			};

            var quantityUnit = _quantityUnitService.GetQuantityUnitById(orderItem.Product.QuantityUnitId);
            model.QuantityUnit = quantityUnit == null ? "" : quantityUnit.GetLocalized(x => x.Name);
            
			if (orderItem.Product.ProductType == ProductType.BundledProduct && orderItem.BundleData.HasValue())
			{
				var bundleData = orderItem.GetBundleData();

				model.BundlePerItemPricing = orderItem.Product.BundlePerItemPricing;
				model.BundlePerItemShoppingCart = bundleData.Any(x => x.PerItemShoppingCart);

				foreach (var bundleItem in bundleData)
				{
					var bundleItemModel = new OrderDetailsModel.BundleItemModel()
					{
						Sku = bundleItem.Sku,
						ProductName = bundleItem.ProductName,
						ProductSeName = bundleItem.ProductSeName,
						VisibleIndividually = bundleItem.VisibleIndividually,
						Quantity = bundleItem.Quantity,
						DisplayOrder = bundleItem.DisplayOrder,
						AttributeInfo = bundleItem.AttributesInfo
					};

					if (model.BundlePerItemShoppingCart)
					{
						decimal priceWithDiscount = _currencyService.ConvertCurrency(bundleItem.PriceWithDiscount, order.CurrencyRate);
						bundleItemModel.PriceWithDiscount = _priceFormatter.FormatPrice(priceWithDiscount, true, order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, false, false);
					}
					
					model.BundleItems.Add(bundleItemModel);
				}
			}

			//unit price, subtotal
			switch (order.CustomerTaxDisplayType)
			{
				case TaxDisplayType.ExcludingTax:
					{
						var unitPriceExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.UnitPriceExclTax, order.CurrencyRate);
						model.UnitPrice = _priceFormatter.FormatPrice(unitPriceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, false, false);

						var priceExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.PriceExclTax, order.CurrencyRate);
						model.SubTotal = _priceFormatter.FormatPrice(priceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, false, false);
					}
					break;
				case TaxDisplayType.IncludingTax:
					{
						var unitPriceInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.UnitPriceInclTax, order.CurrencyRate);
						model.UnitPrice = _priceFormatter.FormatPrice(unitPriceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, true, false);

						var priceInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.PriceInclTax, order.CurrencyRate);
						model.SubTotal = _priceFormatter.FormatPrice(priceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _services.WorkContext.WorkingLanguage, true, false);
					}
					break;
			}
			return model;
		}

        #endregion

        #region Order details

        [RequireHttpsByConfigAttribute(SslRequirement.Yes)]
        public ActionResult Details(int id)
        {
			var order = _orderService.GetOrderById(id);

			if (IsNonExistentOrder(order))
				return HttpNotFound();

			if (IsUnauthorizedOrder(order))
				return new HttpUnauthorizedResult();

            var model = PrepareOrderDetailsModel(order);

            return View(model);
        }

		[RequireHttpsByConfigAttribute(SslRequirement.Yes)]
		public ActionResult Print(int id, bool pdf = false)
		{
			var order = _orderService.GetOrderById(id);

			if (IsNonExistentOrder(order))
				return HttpNotFound();

			if (IsUnauthorizedOrder(order))
				return new HttpUnauthorizedResult();

			var model = PrepareOrderDetailsModel(order);
			var fileName = T("Order.PdfInvoiceFileName", order.Id);

			return PrintCore(new List<OrderDetailsModel> { model }, pdf, fileName);
		}

		[AdminAuthorize]
		public ActionResult PrintMany(string ids = null, bool pdf = false)
		{
			if (!_services.Permissions.Authorize(StandardPermissionProvider.ManageOrders))
				return new HttpUnauthorizedResult();

			IList<Order> orders;

			using (var scope = new DbContextScope(_services.DbContext, autoDetectChanges: false, forceNoTracking: true))
			{
				if (ids != null)
				{
					int[] intIds = ids.ToIntArray();
					orders = _orderService.GetOrdersByIds(intIds);
				}
				else
				{
					orders = _orderService.SearchOrders(0, 0, null, null, null, null, null, null, null, null, 0, int.MaxValue);
				}
			}

			if (orders.Count == 0)
			{
				NotifyInfo(T("Admin.Common.ExportNoData"));
				return RedirectToReferrer();
			}

			if (orders.Count > 500)
			{
				NotifyWarning(T("Admin.Common.ExportToPdf.TooManyItems"));
				return RedirectToReferrer();
			}

			var listModel = orders.Select(x => PrepareOrderDetailsModel(x)).ToList();

			return PrintCore(listModel, pdf, "orders.pdf");
		}

		[NonAction]
		private ActionResult PrintCore(List<OrderDetailsModel> model, bool pdf, string pdfFileName)
		{
			ViewBag.PdfMode = pdf;
			var viewName = "Details.Print";

			if (pdf)
			{
				// TODO: (mc) this is bad for multi-document processing, where orders can originate from different stores.
				var storeId = model[0].StoreId;
				var routeValues = new RouteValueDictionary { { "storeId", storeId } };
				var pdfSettings = _services.Settings.LoadSetting<PdfSettings>(storeId);

				var settings = new PdfConvertSettings
				{
					Size = pdfSettings.LetterPageSizeEnabled ? PdfPageSize.Letter : PdfPageSize.A4,
					Margins = new PdfPageMargins { Top = 35, Bottom = 35 },
					Page = new PdfViewContent(viewName, model, this.ControllerContext),
					Header = new PdfRouteContent("PdfReceiptHeader", "Common", routeValues, this.ControllerContext),
					Footer = new PdfRouteContent("PdfReceiptFooter", "Common", routeValues, this.ControllerContext)
				};

				return new PdfResult(_pdfConverter, settings) { FileName = pdfFileName };
			}

			return View(viewName, model);
		}

        public ActionResult ReOrder(int id)
        {
			var order = _orderService.GetOrderById(id);

			if (IsNonExistentOrder(order))
				return HttpNotFound();

			if (IsUnauthorizedOrder(order))
				return new HttpUnauthorizedResult();

            _orderProcessingService.ReOrder(order);
            return RedirectToRoute("ShoppingCart");
        }

        [HttpPost, ActionName("Details")]
        [FormValueRequired("repost-payment")]
        public ActionResult RePostPayment(int id)
        {
            var order = _orderService.GetOrderById(id);

			if (IsNonExistentOrder(order))
				return HttpNotFound();

			if (IsUnauthorizedOrder(order))
				return new HttpUnauthorizedResult();

			if (_paymentService.CanRePostProcessPayment(order))
			{
				var postProcessPaymentRequest = new PostProcessPaymentRequest
				{
					Order = order,
					IsRePostProcessPayment = true
				};

				_paymentService.PostProcessPayment(postProcessPaymentRequest);

				if (postProcessPaymentRequest.RedirectUrl.HasValue())
				{
					return Redirect(postProcessPaymentRequest.RedirectUrl);
				}
			}
			return RedirectToAction("Details", "Order", new { id = order.Id });
        }

        [RequireHttpsByConfigAttribute(SslRequirement.Yes)]
        public ActionResult ShipmentDetails(int id /* shipmentId */)
        {
            var shipment = _shipmentService.GetShipmentById(id);
            if (shipment == null)
                return HttpNotFound();

            var order = shipment.Order;

			if (IsNonExistentOrder(order))
				return HttpNotFound();

			if (IsUnauthorizedOrder(order))
                return new HttpUnauthorizedResult();

            var model = PrepareShipmentDetailsModel(shipment);

            return View(model);
        }

		private bool IsNonExistentOrder(Order order)
		{
			var flag = order == null || order.Deleted;

			if (!_services.Permissions.Authorize(StandardPermissionProvider.ManageOrders))
			{
				flag = flag || (order.StoreId != 0 && order.StoreId != _services.StoreContext.CurrentStore.Id);
			}

			return flag;
		}

		private bool IsUnauthorizedOrder(Order order)
		{
            if (!_services.Permissions.Authorize(StandardPermissionProvider.ManageOrders))
                return order == null || order.CustomerId != _services.WorkContext.CurrentCustomer.Id;
            else
                return order == null;
		}

        #endregion
    }
}
