using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace AssetManagement.API.Configurations;

public static class MiddlewareConfiguration
{
    public static void ConfigureMiddleware(WebApplication app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            ConfigureSwagger(app);
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCors("AllowReactApp");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }

    private static void ConfigureSwagger(WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Asset Management API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "Asset Management API Documentation";
            c.DefaultModelsExpandDepth(2);
            c.DefaultModelExpandDepth(3);
            c.DisplayRequestDuration();
            c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        });
    }
}