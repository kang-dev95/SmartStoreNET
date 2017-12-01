﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DotLiquid;
using DotLiquid.Exceptions;
using DotLiquid.Util;
using SmartStore.Core.Infrastructure;
using SmartStore.Core.Localization;

namespace SmartStore.Templating.Liquid
{
	public sealed class T : Tag
	{
		private static readonly Regex Syntax = R.B(@"^({0})", DotLiquid.Liquid.QuotedFragment);

		private string _resName;

		public override void Initialize(string tagName, string markup, List<string> tokens)
		{
			Match syntaxMatch = Syntax.Match(markup);

			if (syntaxMatch.Success)
			{
				_resName = syntaxMatch.Groups[1].Value;
			}
			else
			{
				throw new SyntaxException("Syntax Error in 'T' tag - Valid syntax: T '[ResourceName]'.");
			}			

			base.Initialize(tagName, markup, tokens);
		}

		public override void Render(Context context, TextWriter result)
		{
			var resName = (string)context[_resName] ?? _resName;

			var localizer = EngineContext.Current.Resolve<Localizer>();
			var resValue = localizer(resName);

			result.Write(resValue);
		}
	}
}
