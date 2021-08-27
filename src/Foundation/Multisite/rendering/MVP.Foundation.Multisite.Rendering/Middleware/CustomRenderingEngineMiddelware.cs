using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Sitecore.AspNet.RenderingEngine;
using Sitecore.AspNet.RenderingEngine.Configuration;
using Sitecore.Internal;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Request;
using Sitecore.LayoutService.Client.Response;

namespace MVP.Foundation.Multisite.Middleware
{
    public class CustomRenderingEngineMiddelware
    {
        private readonly RequestDelegate _next;

        private readonly ISitecoreLayoutRequestMapper _requestMapper;

        private readonly ISitecoreLayoutClient _layoutService;

        private readonly RenderingEngineOptions _options;

        public CustomRenderingEngineMiddelware(RequestDelegate next, ISitecoreLayoutRequestMapper requestMapper, ISitecoreLayoutClient layoutService, IOptions<RenderingEngineOptions> options)
        {
            _next = next;
            _requestMapper = requestMapper;
            _layoutService = layoutService;
            _options = options.Value;
        }

        public async Task Invoke(HttpContext httpContext, IViewComponentHelper viewComponentHelper, IHtmlHelper htmlHelper)
        {

            if (httpContext.Items.ContainsKey("CustomRenderingEngineMiddelware"))
            {
                throw new ApplicationException("CustomRenderingEngineMiddelware::InvalidRenderingEngineConfiguration");
            }
            if (httpContext.GetSitecoreRenderingContext() == null)
            {
                SitecoreLayoutResponse response = await GetSitecoreLayoutResponse(httpContext).ConfigureAwait(continueOnCapturedContext: false);
                SitecoreRenderingContext renderingContext = new SitecoreRenderingContext
                {
                    Response = response,
                    RenderingHelpers = new RenderingHelpers(viewComponentHelper, htmlHelper)
                };
                httpContext.SetSitecoreRenderingContext(renderingContext);
            }
            else
            {
                httpContext.GetSitecoreRenderingContext().RenderingHelpers = new RenderingHelpers(viewComponentHelper, htmlHelper);
            }
            foreach (Action<HttpContext> postRenderingAction in _options.PostRenderingActions)
            {
                postRenderingAction(httpContext);
            }
            httpContext.Items.Add("CustomRenderingEngineMiddelware", null);
            await _next(httpContext).ConfigureAwait(continueOnCapturedContext: false);
        }

        private async Task<SitecoreLayoutResponse> GetSitecoreLayoutResponse(HttpContext httpContext)
        {
            SitecoreLayoutRequest sitecoreLayoutRequest = _requestMapper.Map(httpContext.Request);
            //Update LayoutRequest Sitename based on hostname from httpcontext
            sitecoreLayoutRequest = ResolveSiteNameBasedOnHost(httpContext, sitecoreLayoutRequest);

            return await _layoutService.Request(sitecoreLayoutRequest).ConfigureAwait(continueOnCapturedContext: false);
        }

        private SitecoreLayoutRequest ResolveSiteNameBasedOnHost(HttpContext httpContext, SitecoreLayoutRequest sitecoreLayoutRequest)
        {
            Dictionary<string, string>? sites;
            string siteName = "", currentSitename = "";
            //Getting private _layoutRequestOptions and then accessing SitecoreLayoutRequestOptions.RequestDefaults
            FieldInfo field = typeof(DefaultLayoutClient).GetField
            ("_layoutRequestOptions", BindingFlags.Instance | BindingFlags.NonPublic);
            IOptionsSnapshot<SitecoreLayoutRequestOptions> sitecoreLayoutRequestOptions = (IOptionsSnapshot<SitecoreLayoutRequestOptions>)field.GetValue(_layoutService);

            if (sitecoreLayoutRequestOptions.Value.RequestDefaults.TryReadValue<Dictionary<string, string>?>("sc_sites", out sites) && sitecoreLayoutRequestOptions.Value.RequestDefaults.TryReadValue<string>("sc_site", out currentSitename))
            {
                string hostName = httpContext.Request.Host.Value;
                siteName = sites.ContainsKey(hostName) ? sites[hostName] : "";

                if (currentSitename.ToLower() != siteName.ToLower())
                {
                    sitecoreLayoutRequestOptions.Value.RequestDefaults["sc_site"] = siteName;
                    //finaly update sitecorelayout request defaults
                    field.SetValue(_layoutService, sitecoreLayoutRequestOptions);
                    //also update in layoutrequest
                    sitecoreLayoutRequest.SiteName(siteName);
                }
            }
            return sitecoreLayoutRequest;
        }
    }
}
