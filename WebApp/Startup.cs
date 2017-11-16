using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using WebApp.Extensions;
using WebApp.Identity.Extensions;
using WebApp.Localisation.Extensions;
using WebApp.Middleware;

namespace WebApp
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
            
            // Configure api gateway
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            // Configure business layer
            services.ConfigureIdentity(this.Configuration.GetConnectionString("IdentityConnection"));
            services.ConfigureLocalisation(this.Configuration.GetConnectionString("LocalisationConnection"));

            // Configure AutoMapper for API Gateway
            services.ConfigureAutoMapper();

            // Add framework services.
            services.AddMvc();
            services.AddOptions();

            // Configure Swagger
            services.ConfigureSwaggerGen(options =>
                options.CustomSchemaIds(schemaId => schemaId.FullName)
            );

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info {Title = "angularXcore API", Version = "v1"});
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
            app.ConfigureLocalisation();

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
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "angularXcore v1"); });
        }
    }
}