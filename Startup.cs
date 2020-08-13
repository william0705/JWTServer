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
            //������ݱ���API
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo("data_keys"))//���Ĵ洢�ļ�·��
                .SetDefaultKeyLifetime(TimeSpan.FromDays(30))//������������
                .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration()//���ļ����㷨��ɢ���㷨
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
                }).SetApplicationName("shared app name");//��ͬ����֮��������ͬ�����ƣ���������ͬĿ�ĵ�protection,�����ǲ�ͬ�������Ա˴˽���
            
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
            //Configuration.GetConnectionString("DefaultConnection")����?�Ҳ���null
            services.AddDbContext<LibraryDbContext>(options => 
                options.UseSqlServer(defaultConnection, 
                builder => builder.MigrationsAssembly(typeof(Startup).Assembly.GetName().Name)));
            services.AddIdentity<User, Role>().AddEntityFrameworkStores<LibraryDbContext>();
            services.AddControllers();

            services.AddCors(options =>
            {
                //Origin������������ı���Ŀ��Դ��Դ��Э��https+����+�˿ڣ���߲���Я��б��/����
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

            //�����м����������Swagger��ΪJSON�ս��
            app.UseSwagger();
            //�����м�������swagger-ui��ָ��Swagger JSON�ս��
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseRouting();

            //CORS�м��Ӧ������κο��ܻ��õ�CORS���ܵ��м��֮ǰ
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
