using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using WebUI.Filters;
using WebUI.Service;

namespace WebUI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<SeamCarverContext>(options => options.UseSqlServer(Configuration.GetConnectionString("default")));

            //services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            //    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"), subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: true);

            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(o =>
                {
                    // get data from appsettings
                    o.Instance = "https://login.microsoftonline.com/";
                    o.Domain = "jjanos.onmicrosoft.com";
                    o.ClientId = "5421169e-45de-41f1-b2d8-7fe5a2acbcb7";
                    o.TenantId = "common";
                    o.CallbackPath = "/signin-oidc";
                    o.Events.OnTokenValidated = validatedCtx => CheckAndMarkNewUser(validatedCtx);
                }, subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: true);



            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
                options.Filters.Add<NewUserActionFilter>();
            });

            services.AddRazorPages()
                .AddMicrosoftIdentityUI();

            services.AddSingleton(typeof(FileSystemHelper), sp =>
            {
                var webRoot = ((IWebHostEnvironment)sp.GetRequiredService<IWebHostEnvironment>()).WebRootPath;
                var uploadBaseVirtual = "Uploads";
                var uploadBasePhysical = Path.Combine(webRoot, uploadBaseVirtual);
                return new FileSystemHelper(uploadBasePhysical, uploadBaseVirtual);
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (env.IsDevelopment())
            {// only for debugging http message flow
                app.Use((context, next) =>
               {
                   var ret = next.Invoke();
                   return ret;
               });
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Image}/{action=Index}");
                endpoints.MapRazorPages();
            });
        }

        private async Task CheckAndMarkNewUser(TokenValidatedContext validatedContext)
        {
            var userObjectId = validatedContext.Principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            using (var db = validatedContext.HttpContext.RequestServices.GetRequiredService<SeamCarverContext>())
            {
                var user = db.Users.Find(new Guid(userObjectId));

                if (user == null)
                    //validatedContext.Principal.AddIdentity(new ClaimsIdentity(new Claim[] { new Claim("isNewUser", "true") }));
                    ((ClaimsIdentity)validatedContext.Principal.Identity).AddClaim(new Claim("isNewUser", "true"));
            }
            await Task.CompletedTask;
        }
    }
}
