using System;
using AspNet.Security.OAuth.Validation;
using AspNet.Security.OpenIdConnect.Primitives;
using AutoMapper;
using Common.Middleware;
using Identity.Data;
using Identity.Data.Models;
using Identity.Extensions;
using Identity.Profiles;
using IdentityProvider.Profiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace IdentityProvider
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            services.AddDbContext<IdentityDbContext>(
                options =>
                {
                    options.UseSqlServer(this.Configuration.GetConnectionString("IdentityConnection"));
                    options.UseOpenIddict();
                });

            // Add Identity
            services.AddIdentity<ApplicationUser, ApplicationRole>(o =>
            {
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = true;
                o.Password.RequiredLength = 6;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                o.SignIn.RequireConfirmedEmail = false;
                o.SignIn.RequireConfirmedPhoneNumber = false;
                o.User.RequireUniqueEmail = true;
            })
                .AddEntityFrameworkStores<IdentityDbContext>()
                .AddDefaultTokenProviders();

            // Configure Identity to use the same JWT claims as OpenIddict instead of the legacy WS-Federation claims it uses by default (ClaimTypes),
            // which saves you from doing the mapping in your authorization controller.
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                options.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Username;
                options.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;
            });

            services.AddOpenIddict(options =>
            {
                options.AddEntityFrameworkCoreStores<IdentityDbContext>();

                options.AddMvcBinders();

                options.EnableTokenEndpoint("/api/auth/token");
                //options.EnableAuthorizationEndpoint("/api/auth/authorize");
                //options.EnableLogoutEndpoint("/api/auth/logout");
                options.EnableIntrospectionEndpoint("/connect/introspect");

                options.AllowPasswordFlow();
                options.AllowClientCredentialsFlow();
                //options.AllowImplicitFlow();

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(7));

                // During development, you can disable the HTTPS requirement.
                options.DisableHttpsRequirement();

                // Register a new ephemeral key, that is discarded when the application
                // shuts down. Tokens signed using this key are automatically invalidated.
                // This method should only be used during development.
                // Note: to use JWT access tokens instead of the default
                // encrypted format, the following lines are required:
                //
                // options.UseJsonWebTokens();
                // options.AddEphemeralSigningKey();
            });

            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = OAuthValidationDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = OAuthValidationDefaults.AuthenticationScheme;
            }).AddOAuthValidation();

            // Configure api gateway
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Configure automapper
            services.AddAutoMapper(typeof(IdentityProviderProfile), typeof(IdentityProfile));

            // Configure business layer
            services.ConfigureIdentity();

            // Add framework services.
            services.AddMvc();
            services.AddOptions();

            // Configure Swagger
            services.ConfigureSwaggerGen(options =>
                options.CustomSchemaIds(schemaId => schemaId.FullName)
            );

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info {Title = "Identity Provider API", Version = "v1"});
                c.AddSecurityDefinition("OpenIdDict", new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "password",
                    TokenUrl = "/api/auth/token"
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // Configure business layer
            app.ConfigureIdentity();

            app.UseCors(builder =>
                builder.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin()
                    .WithExposedHeaders("Content-Disposition", "Content-Type")
            );

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                await next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            // Configure Middleware
            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
            app.UseMiddleware<GlobalTraceMiddleware>();

            app.UseMvcWithDefaultRoute();

            // Configure Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Provider API v1"); });
        }
    }
}