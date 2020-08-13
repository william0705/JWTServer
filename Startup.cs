using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JWTServer.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
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

        private static string MyCorsPolicy = "CorsPolicy";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //添加数据保护API
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo("data_keys"))//更改存储文件路径
                .SetDefaultKeyLifetime(TimeSpan.FromDays(30))//更改生命周期
                .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration()//更改加密算法和散列算法
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
                }).SetApplicationName("shared app name");//不同程序之间设置相同的名称，并创建相同目的的protection,可以是不同程序间可以彼此解密
            
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

            services.AddCors(options =>
            {
                //Origin：被允许请求的本项目资源的源（协议https+域名+端口（后边不能携带斜杠/））
                //options.AddPolicy("AllowAllMethodsPolicy",builder=>builder.WithOrigins("https://localhost:6001").AllowAnyMethod());
                //options.AddPolicy("AllAnyOriginPolicy",builder=>builder.AllowAnyOrigin());
                //options.AddDefaultPolicy(builder=>builder.WithOrigins("https://localhost:6001"));
                options.AddPolicy(MyCorsPolicy,builder=>builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
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

            //CORS中间件应添加在任何可能会用到CORS功能的中间件之前
            //app.UseCors(builder => builder.WithOrigins("http://localhost:5000"));
            app.UseCors("AllowAllMethodsPolicy");

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireCors(MyCorsPolicy);
            });
        }
    }
}
