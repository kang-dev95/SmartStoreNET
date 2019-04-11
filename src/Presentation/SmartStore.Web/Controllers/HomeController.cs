﻿using System;
using System.Web.Mvc;
using SmartStore.Core.Domain.Common;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Email;
using SmartStore.Services;
using SmartStore.Services.Catalog;
using SmartStore.Services.Customers;
using SmartStore.Services.Localization;
using SmartStore.Services.Messages;
using SmartStore.Services.Search;
using SmartStore.Services.Topics;
using SmartStore.Web.Framework.Controllers;
using SmartStore.Web.Framework.Filters;
using SmartStore.Web.Framework.Security;
using SmartStore.Web.Models.Common;

namespace SmartStore.Web.Controllers
{
	public partial class HomeController : PublicControllerBase
	{
		private readonly Lazy<ICategoryService> _categoryService;
		private readonly Lazy<IProductService> _productService;
		private readonly Lazy<IManufacturerService> _manufacturerService;
		private readonly Lazy<ICatalogSearchService> _catalogSearchService;
		private readonly Lazy<CatalogHelper> _catalogHelper;
		private readonly Lazy<ITopicService> _topicService;
		private readonly Lazy<CaptchaSettings> _captchaSettings;
		private readonly Lazy<CommonSettings> _commonSettings;
		private readonly Lazy<CustomerSettings> _customerSettings;
		private readonly Lazy<PrivacySettings> _privacySettings;

		public HomeController(
			Lazy<ICategoryService> categoryService,
			Lazy<IProductService> productService,
			Lazy<IManufacturerService> manufacturerService,
			Lazy<ICatalogSearchService> catalogSearchService,
			Lazy<CatalogHelper> catalogHelper,
			Lazy<ITopicService> topicService,
			Lazy<CaptchaSettings> captchaSettings,
			Lazy<CommonSettings> commonSettings,
			Lazy<CustomerSettings> customerSettings,
			Lazy<PrivacySettings> privacySettings)
        {
			_categoryService = categoryService;
			_productService = productService;
			_manufacturerService = manufacturerService;
			_catalogSearchService = catalogSearchService;
			_catalogHelper = catalogHelper;
			_topicService = topicService;
			_captchaSettings = captchaSettings;
			_commonSettings = commonSettings;
            _customerSettings = customerSettings;
			_privacySettings = privacySettings;
		}
		
        [RequireHttpsByConfig(SslRequirement.No)]
        public ActionResult Index()
        {
			return View();
        }

		public ActionResult StoreClosed()
		{
			return View();
		}

		[RequireHttpsByConfig(SslRequirement.No)]
		[GdprConsent]
		public ActionResult ContactUs()
		{
			var topic = _topicService.Value.GetTopicBySystemName("ContactUs", 0, false);

            var model = new ContactUsModel
			{
				Email = Services.WorkContext.CurrentCustomer.Email,
				FullName = Services.WorkContext.CurrentCustomer.GetFullName(),
				FullNameRequired = _privacySettings.Value.FullNameOnContactUsRequired,
				DisplayCaptcha = _captchaSettings.Value.Enabled && _captchaSettings.Value.ShowOnContactUsPage,
                MetaKeywords = topic?.GetLocalized(x => x.MetaKeywords),
                MetaDescription = topic?.GetLocalized(x => x.MetaDescription),
                MetaTitle = topic?.GetLocalized(x => x.MetaTitle),
            };

			var publisher = Services.Resolve<SmartStore.Core.Events.IMessagePublisher>();
			publisher.Publish(new Core.Events.AppStartedEvent());

			return View(model);
		}

		[HttpPost, ActionName("ContactUs")]
		[ValidateCaptcha, ValidateHoneypot]
		[GdprConsent]
		public ActionResult ContactUsSend(ContactUsModel model, bool captchaValid)
		{
			// Validate CAPTCHA
			if (_captchaSettings.Value.Enabled && _captchaSettings.Value.ShowOnContactUsPage && !captchaValid)
			{
				ModelState.AddModelError("", T("Common.WrongCaptcha"));
			}

			if (ModelState.IsValid)
			{
				var customer = Services.WorkContext.CurrentCustomer;
				var email = model.Email.Trim();
				var fullName = model.FullName;
				var subject = T("ContactUs.EmailSubject", Services.StoreContext.CurrentStore.Name);
				var body = Core.Html.HtmlUtils.ConvertPlainTextToHtml(model.Enquiry.HtmlEncode());

				// Required for some SMTP servers
				EmailAddress sender = null;
				if (!_commonSettings.Value.UseSystemEmailForContactUsForm)
				{
					sender = new EmailAddress(email, fullName);
				}

				// email
				var msg = Services.MessageFactory.SendContactUsMessage(customer, email, fullName, subject, body, sender);

				if (msg?.Email?.Id != null)
				{
					model.SuccessfullySent = true;
					model.Result = T("ContactUs.YourEnquiryHasBeenSent");
					Services.CustomerActivity.InsertActivity("PublicStore.ContactUs", T("ActivityLog.PublicStore.ContactUs"));
				}
				else
				{
					ModelState.AddModelError("", T("Common.Error.SendMail"));
					model.Result = T("Common.Error.SendMail");
				}

				return View(model);
			}

			model.DisplayCaptcha = _captchaSettings.Value.Enabled && _captchaSettings.Value.ShowOnContactUsPage;
			return View(model);
		}

		[RequireHttpsByConfigAttribute(SslRequirement.No)]
		public ActionResult Sitemap()
		{
            return RedirectPermanent(Services.StoreContext.CurrentStore.Url);
		}
    }
}
