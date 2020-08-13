using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JWTServer.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace JWTServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataProtection();
            var tokenSection = Configuration.GetSection("Security:Token");

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(options =>
                {
                    options.IncludeErrorDetails = true;
                    options.TokenValidationParameters=new TokenValidationParameters()
                    {
                        ValidateAudience = true,ValidateLifetime = true,ValidateIssuer = true,ValidateIssuerSigningKey = true,
                        ValidIssuer = tokenSection["Issuer"],
                        ValidAudience = tokenSection["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSection["key"])),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Title = "My API",
                    Version = "v1"
                });
            });

            var defaultConnection =
                "Server=(localdb)\\MSSQLLocalDB;Database=Library;Trusted_Connection=True;MultipleActiveResultSets=true";
            //Configuration.GetConnectionString("DefaultConnection")报错?找不到null
            services.AddDbContext<LibraryDbContext>(options => 
                options.UseSqlServer(defaultConnection, 
                builder => builder.MigrationsAssembly(typeof(Startup).Assembly.GetName().Name)));
            services.AddIdentity<User, Role>().AddEntityFrameworkStores<LibraryDbContext>();
            services.AddControllers();
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
                app.UseExceptionHandler();
                app.UseHsts();
            }
            app.UseHttpsRedirection();

            //启用中间件服务生成Swagger作为JSON终结点
            app.UseSwagger();
            //启用中间件服务对swagger-ui，指定Swagger JSON终结点
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
