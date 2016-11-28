﻿using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web.Mvc;
using SmartStore.Utilities;

namespace SmartStore.Web.Framework.Theming
{
	public class ThemeableRazorViewEngine : ThemeableVirtualPathProviderViewEngine
	{
		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		public ThemeableRazorViewEngine()
		{
			var areaBasePathsSetting = CommonHelper.GetAppSetting<string>("sm:AreaBasePaths", "~/Plugins/");
			var areaBasePaths = areaBasePathsSetting.Split(',').Select(x => x.Trim().EnsureEndsWith("/")).ToArray();

			// 0: view, 1: controller, 2: area
			// {0} is appended by ExpandLocationFormats()
			var areaFormats = new[] 
			{
				"{2}/Views/{1}/",
				"{2}/Views/Shared/"
			};
			var areaLocationFormats = areaBasePaths.SelectMany(x => areaFormats.Select(f => x + f));

			AreaViewLocationFormats = ExpandLocationFormats(areaLocationFormats, ViewType.View).ToArray();
			AreaMasterLocationFormats = ExpandLocationFormats(areaLocationFormats, ViewType.Layout).ToArray();
			AreaPartialViewLocationFormats = ExpandLocationFormats(areaLocationFormats, ViewType.Partial).ToArray();

			// 0: view, 1: controller, 2: theme
			// {0} is appended by ExpandLocationFormats()
			var locationFormats = new[]
            {
                "~/Themes/{2}/Views/{1}/",
                "~/Views/{1}/",
                "~/Themes/{2}/Views/Shared/",
                "~/Views/Shared/"
            };

            ViewLocationFormats = ExpandLocationFormats(locationFormats, ViewType.View).ToArray();
            MasterLocationFormats = ExpandLocationFormats(locationFormats, ViewType.Layout).ToArray();
			PartialViewLocationFormats = ExpandLocationFormats(locationFormats, ViewType.Partial).ToArray();

			if (EnableVbViews)
            {
                FileExtensions = new[] { "cshtml", "vbhtml" };
            }
            else
            {
                FileExtensions = new[] { "cshtml" };
            }
		}

		protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
		{
			return new RazorView(controllerContext, partialPath, null, false, base.FileExtensions, base.ViewPageActivator);
		}

		protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
		{
			return new RazorView(controllerContext, viewPath, masterPath, true, base.FileExtensions, base.ViewPageActivator);
		}
	}
}
