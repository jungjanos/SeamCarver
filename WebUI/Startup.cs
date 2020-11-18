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
using Microsoft.AspNetCore.Http;
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
        private string _webroot;
        private string _userFolderPhysicalBase;
        private string _userFolderVirtualBase;

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment webhost)
        {
            Configuration = configuration;
            _webroot = webhost.WebRootPath;
            _userFolderPhysicalBase = Path.Combine(_webroot, Configuration.GetValue<string>("UserFolderBase"));
            _userFolderVirtualBase = Configuration.GetValue<string>("UserFolderBase");

            if (!Directory.Exists(_userFolderPhysicalBase)) // put this in a file helper
                Directory.CreateDirectory(_userFolderPhysicalBase);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
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
                    //o.ClaimActions.Remove("iss");
                    //o.ClaimActions.Remove("http://schemas.microsoft.com/identity/claims/tenantid");
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

            services.AddAuthorization(options =>
            // authz policy to check claim that the signed principal does not yet have account in the application
            {
                options.AddPolicy("HasNoAccount", policy => policy.RequireClaim("hasAccount", new[] { "false" }));
                options.AddPolicy("HasAccount", policy => policy.RequireClaim("hasAccount", new[] { "true" }));
            });

            services.AddScoped(typeof(UserFileSystemHelper), sp =>
            {
                var principal = sp.GetService<IHttpContextAccessor>().HttpContext.User;

                var userFolder = principal?.Claims?.FirstOrDefault(c => c.Type == "LocalFolder")?.Value;

                return new UserFileSystemHelper(_userFolderPhysicalBase, _userFolderVirtualBase, userFolder);
            });

            services.AddScoped<IUserService>(services =>
            {
                var db = services.GetRequiredService<SeamCarverContext>();
                return new UserService(db, _userFolderPhysicalBase);
            });

            services.AddScoped<ActionHistoryPersister>();
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
                var user = await db.Users.FindAsync(new Guid(userObjectId));

                if (user == null)
                    ((ClaimsIdentity)validatedContext.Principal.Identity).AddClaim(new Claim("hasAccount", "false"));
                else
                {
                    ((ClaimsIdentity)validatedContext.Principal.Identity).AddClaim(new Claim("hasAccount", "true"));
                    ((ClaimsIdentity)validatedContext.Principal.Identity).AddClaim(new Claim("LocalFolder", user.LocalFolder));
                }
            }
        }
    }
}
