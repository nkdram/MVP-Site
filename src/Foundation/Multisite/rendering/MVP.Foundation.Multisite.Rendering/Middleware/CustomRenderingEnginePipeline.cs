using Microsoft.AspNetCore.Builder;
using Sitecore.AspNet.RenderingEngine.Extensions;
using Sitecore.AspNet.RenderingEngine.Middleware;
using Sitecore.Internal;
using System;


namespace MVP.Foundation.Multisite.Middleware
{
    public class CustomRenderingEnginePipeline : RenderingEnginePipeline
    {
        public override void Configure(IApplicationBuilder app)
        {
            //Register Custom Rendering Engine Middleware
            app.UseMiddleware<CustomRenderingEngineMiddelware>(Array.Empty<object>());
        }
    }
}
