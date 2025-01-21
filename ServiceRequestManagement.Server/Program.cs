
using Azure.Core;
using System.Net;
namespace ServiceRequestManagement.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            // Add CORS configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp",
                    policy => policy.WithOrigins("https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/") // Add your React app's URL
                                    .AllowAnyHeader()
                                    .AllowAnyMethod());
            });
            var app = builder.Build();

            // Use CORS
            app.UseCors("AllowReactApp");

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment() || app.Environment.IsProduction()) // Enable Swagger for production
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Service Request Management API V1");
                    c.RoutePrefix = "swagger"; // Makes Swagger available at /swagger/index.html
                });
            }

            app.UseHttpsRedirection();

            //app.UseAuthorization();


            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}
