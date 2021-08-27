using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Sitecore.Internal;
using Sitecore.LayoutService.Client.Request;

namespace MVP.Foundation.Multisite.Extensions
{
	public static class LayoutRequestExtensions
	{
		public static Dictionary<string, string>? Sites(this SitecoreLayoutRequest request)
		{
			return ReadValue<Dictionary<string, string>?>(request, "sc_sites");
		}

		public static SitecoreLayoutRequest Sites(this SitecoreLayoutRequest request, Dictionary<string, string>? value)
		{
			return WriteValue(request, "sc_sites", value);
		}
		private static T ReadValue<T>(SitecoreLayoutRequest request, string key)
		{
			if (request.TryReadValue<T>(key, out var value))
			{
				return value;
			}
			return default(T);
		}

		private static SitecoreLayoutRequest WriteValue<T>(SitecoreLayoutRequest request, string key, [AllowNull] T value)
		{
			request[key] = value;
			return request;
		}
	}
}
